using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class BotFollowsUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectBotFollowsUserBox;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }

        public int TickCount { get; set; }

        private int _delay;
        private int _mode = 0; // 0 = stop following, 1 = start following
        private string _botName = "";
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
        private bool _requested;
        private Habbo _pendingActor;

        public BotFollowsUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
        }

        // ── IWiredCustomData ───────────────────────────────────────────────────

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                bot_name = _botName ?? "",
                mode = _mode,
                delay = Delay,
                userSource = _userSource,
                botSource = _botSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _botName = "";
            _mode = 0;
            _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _botName = data.bot_name ?? "";
                _mode = data.mode;
                _userSource = data.userSource;
                _botSource = WiredBotSourceUtil.NormalizeBotSource(
                                  data.botSource ?? WiredBotSourceUtil.SOURCE_BOT_NAME);
                Delay = data.delay;
            }
            else if (wiredData.Contains('\t'))
            {
                // Retrocompatibilidad Java legacy: "delay\tmode\tbotName"
                var tabs = wiredData.Split('\t');
                if (tabs.Length == 3)
                {
                    if (int.TryParse(tabs[0], out int delay)) Delay = delay;
                    _mode = tabs[1] == "1" ? 1 : 0;
                    _botName = tabs[2];
                }

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
                _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }
            else
            {
                // Retrocompatibilidad C# viejo: "mode;userSource;botSource;botName"
                var parts = wiredData.Split(';');
                if (parts.Length == 4)
                {
                    int.TryParse(parts[0], out _mode);
                    int.TryParse(parts[1], out _userSource);
                    _botSource = WiredBotSourceUtil.NormalizeBotSource(
                        int.TryParse(parts[2], out int bs) ? bs : WiredBotSourceUtil.SOURCE_BOT_NAME);
                    _botName = parts[3];
                }
            }

            StringData = $"{_mode};{_userSource};{_botSource};{_botName}";
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            _mode = intCount > 0
                ? packet.PopInt()
                : 0;
            _userSource = intCount > 1
                ? packet.PopInt()
                : WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = intCount > 2
                ? WiredBotSourceUtil.NormalizeBotSource(packet.PopInt())
                : WiredBotSourceUtil.SOURCE_BOT_NAME;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            _botName = packet.PopString().Replace("\t", "");
            Delay = packet.PopInt();

            StringData = $"{_mode};{_userSource};{_botSource};{_botName}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furni seleccionable
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(_botName ?? "");
            packet.WriteInteger(3); // mode, userSource, botSource
            packet.WriteInteger(_mode);
            packet.WriteInteger(_userSource);
            packet.WriteInteger(_botSource);
            packet.WriteInteger(1); // requiresTriggeringUser hint
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(_botName)) return false;

            _pendingActor = Params.Length > 0 ? Params[0] as Habbo : null;

            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null || !_requested) return false;

            _requested = false;

            var bots = WiredBotSourceUtil.ResolveBots(Instance, _botSource, _botName);
            if (bots == null || bots.Count == 0)
            {
                _pendingActor = null;
                return false;
            }

            // Resolver usuario destino
            RoomUser target = null;
            if (_userSource == WiredSourceUtil.SOURCE_TRIGGER && _pendingActor != null)
                target = Instance.GetRoomUserManager().GetRoomUserByHabbo(_pendingActor.Id);

            foreach (var bot in bots)
            {
                if (bot.RoomId != Instance.RoomId) continue;

                if (_mode == 1 && target != null)
                {
                    // Iniciar seguimiento
                    bot.BotData.ForcedUserTargetMovement = _pendingActor.Id;
                    if (bot.IsWalking) bot.ClearMovement(true);
                    bot.MoveTo(target.X, target.Y);
                }
                else
                {
                    // Detener seguimiento
                    bot.BotData.ForcedUserTargetMovement = 0;
                    if (bot.IsWalking) bot.ClearMovement(true);
                }
            }

            _pendingActor = null;
            return true;
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string bot_name { get; set; }
            public int mode { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
            public int? botSource { get; set; }
        }
    }
}