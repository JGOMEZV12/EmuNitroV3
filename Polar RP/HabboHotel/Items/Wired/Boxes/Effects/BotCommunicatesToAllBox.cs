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
    internal class BotCommunicatesToAllBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectBotCommunicatesToAllBox;
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
        private int _mode = 0; // 0 = talk, 1 = shout
        private string _botName = "";
        private string _message = "";
        private int _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
        private bool _requested;

        public BotCommunicatesToAllBox(Room instance, Item item)
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
                botSource = _botSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _botName = "";
            _message = "";
            _mode = 0;
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
                _botSource = WiredBotSourceUtil.NormalizeBotSource(
                                 data.botSource ?? WiredBotSourceUtil.SOURCE_BOT_NAME);
                Delay = data.delay;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "mode;botSource;botName;message"
                var parts = wiredData.Split(';');
                if (parts.Length >= 4)
                {
                    int.TryParse(parts[0], out _mode);
                    _botSource = WiredBotSourceUtil.NormalizeBotSource(
                        int.TryParse(parts[1], out int bs) ? bs : WiredBotSourceUtil.SOURCE_BOT_NAME);
                    _botName = parts[2];
                    _message = parts[3];
                }
                // Formato tab-separated del Java viejo: "delay\tmode\tbotName\tmessage"
                else if (wiredData.Contains('\t'))
                {
                    var tabs = wiredData.Split('\t');
                    if (tabs.Length == 4)
                    {
                        if (int.TryParse(tabs[0], out int delay)) Delay = delay;
                        _mode = tabs[1] == "1" ? 1 : 0;
                        _botName = tabs[2];
                        _message = tabs[3];
                    }
                }

                _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }

            // Sincronizar StringData para compatibilidad con sistemas que lo lean
            StringData = $"{_mode};{_botSource};{_botName};{_message}";
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            _mode = intCount > 0
                ? packet.PopInt()
                : 0;
            _botSource = intCount > 1
                ? WiredBotSourceUtil.NormalizeBotSource(packet.PopInt())
                : WiredBotSourceUtil.SOURCE_BOT_NAME;
            for (int i = 2; i < intCount; i++) packet.PopInt();

            // El cliente envía "botName\tmessage"
            string chatConfig = packet.PopString();
            var parts = chatConfig.Split('\t');
            _botName = parts.Length > 0 ? parts[0] : "";
            _message = parts.Length > 1 ? parts[1] : "";

            Delay = packet.PopInt();

            StringData = $"{_mode};{_botSource};{_botName};{_message}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furni seleccionable
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString($"{_botName}\t{_message}"); // formato esperado por el cliente
            packet.WriteInteger(2);
            packet.WriteInteger(_mode);
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

            // Resolver placeholders del actor si está disponible
            string resolvedMessage = _message;
            if (Params.Length > 0 && Params[0] is Habbo habbo)
            {
                resolvedMessage = resolvedMessage
                    .Replace("%username%", habbo.Username)
                    .Replace("%credits%", habbo.Credits.ToString())
                    .Replace("%roomname%", Instance.RoomData?.Name ?? "")
                    .Replace("%owner%", Instance.RoomData?.OwnerName ?? "")
                    .Replace("%user_count%", Instance.GetRoomUserManager().userCount.ToString());
            }

            // Guardar el mensaje resuelto para OnCycle
            StringData = $"{_mode};{_botSource};{_botName};{resolvedMessage}";

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

            // Leer el mensaje resuelto que Execute guardó
            var parts = StringData?.Split(';');
            if (parts == null || parts.Length < 4) return false;

            int.TryParse(parts[0], out int mode);
            string message = parts[3];

            var bots = WiredBotSourceUtil.ResolveBots(Instance, _botSource, _botName);
            if (bots == null || bots.Count == 0) return false;

            foreach (var bot in bots)
            {
                if (bot.RoomId != Instance.RoomId) continue;

                if (mode == 1)
                    bot.Chat(message, true, 2, "black");
                else
                    bot.Chat(message, false, 2, "black");
            }

            // Restaurar StringData al formato estable (sin el mensaje resuelto temporal)
            StringData = $"{_mode};{_botSource};{_botName};{_message}";
            return true;
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string bot_name { get; set; }
            public int mode { get; set; }
            public string message { get; set; }
            public int delay { get; set; }
            public int? botSource { get; set; }
        }
    }
}