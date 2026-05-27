using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class BotCommunicatesToUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectBotCommunicatesToUserBox;
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
        private int _mode = 0; // 0 = talk (con prefijo usuario), 1 = whisper
        private string _botName = "";
        private string _message = "";
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
        private bool _requested;

        // Actor capturado en Execute para usarlo en OnCycle
        private Habbo _pendingActor;

        public BotCommunicatesToUserBox(Room instance, Item item)
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
                message = _message ?? "",
                delay = Delay,
                userSource = _userSource,
                botSource = _botSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _botName = "";
            _message = "";
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
                _message = data.message ?? "";
                _userSource = data.userSource;
                _botSource = WiredBotSourceUtil.NormalizeBotSource(
                                  data.botSource ?? WiredBotSourceUtil.SOURCE_BOT_NAME);
                Delay = data.delay;
            }
            else if (wiredData.Contains('\t'))
            {
                // Retrocompatibilidad Java legacy: "delay\tmode\tbotName\tmessage"
                var tabs = wiredData.Split('\t');
                if (tabs.Length == 4)
                {
                    if (int.TryParse(tabs[0], out int delay)) Delay = delay;
                    _mode = tabs[1] == "1" ? 1 : 0;
                    _botName = tabs[2];
                    _message = tabs[3];
                }

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
                _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }
            else
            {
                // Retrocompatibilidad C# viejo: StringData era "botName\tmessage"
                // (HandleSave anterior guardaba directamente el string del cliente)
                var parts = wiredData.Split('\t');
                if (parts.Length == 2)
                {
                    _botName = parts[0];
                    _message = parts[1];
                }

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
                _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }

            StringData = $"{_botName}\t{_message}";
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

            // El cliente envía "botName\tmessage"
            string chatConfig = packet.PopString();
            var parts = chatConfig.Split('\t');
            _botName = parts.Length > 0 ? parts[0] : "";
            _message = parts.Length > 1 ? parts[1] : "";

            Delay = packet.PopInt();

            StringData = $"{_botName}\t{_message}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furni seleccionable
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString($"{_botName}\t{_message}");
            packet.WriteInteger(3); // tres parámetros int: mode, userSource, botSource
            packet.WriteInteger(_mode);
            packet.WriteInteger(_userSource);
            packet.WriteInteger(_botSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(_botName)) return false;
            if (string.IsNullOrEmpty(_message)) return false;

            // Capturar el actor para OnCycle
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

            // Resolver usuarios destino
            var targets = ResolveTargetUsers();
            if (targets.Count == 0)
            {
                _pendingActor = null;
                return false;
            }

            foreach (var habbo in targets)
            {
                if (habbo?.GetClient() == null) continue;

                // Resolver placeholders por usuario
                string resolved = _message
                    .Replace("%username%", habbo.Username)
                    .Replace("%credits%", habbo.Credits.ToString())
                    .Replace("%roomname%", Instance.RoomData?.Name ?? "")
                    .Replace("%owner%", Instance.RoomData?.OwnerName ?? "")
                    .Replace("%user_count%", Instance.GetRoomUserManager().userCount.ToString());

                foreach (var bot in bots)
                {
                    if (bot.RoomId != Instance.RoomId) continue;

                    string botMessage = resolved.Replace("%name%", bot.BotData?.Name ?? _botName);

                    if (_mode == 1)
                    {
                        // whisper: solo lo ve el usuario destino
                        habbo.GetClient().SendWhisper($"{bot.BotData?.Name ?? _botName}: {botMessage}");
                    }
                    else
                    {
                        // talk público con prefijo "username: message"
                        bot.Chat($"{habbo.Username}: {botMessage}", false, 2, "black");
                    }
                }
            }

            _pendingActor = null;
            return true;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private List<Habbo> ResolveTargetUsers()
        {
            var result = new List<Habbo>();

            if (_userSource == WiredSourceUtil.SOURCE_TRIGGER || _userSource == WiredSourceUtil.SOURCE_CLICKED_USER)
            {
                if (_pendingActor != null)
                    result.Add(_pendingActor);
            }
            else
            {
                // SOURCE_SELECTED u otros: todos los usuarios de la sala
                foreach (var ru in Instance.GetRoomUserManager().GetRoomUsers())
                {
                    if (ru == null || ru.IsBot) continue;
                    var h = ru.GetClient()?.GetHabbo();
                    if (h != null) result.Add(h);
                }
            }

            return result;
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string bot_name { get; set; }
            public int mode { get; set; }
            public string message { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
            public int? botSource { get; set; }
        }
    }
}