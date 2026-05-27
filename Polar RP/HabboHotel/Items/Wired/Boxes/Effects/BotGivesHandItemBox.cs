using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class BotGivesHandItemBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectBotGivesHanditemBox;
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
        private string _botName = "";
        private int _itemId = 0;
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
        private bool _requested;
        private Habbo _pendingActor;

        public BotGivesHandItemBox(Room instance, Item item)
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
                item_id = _itemId,
                delay = Delay,
                userSource = _userSource,
                botSource = _botSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _botName = "";
            _itemId = 0;
            _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _botName = data.bot_name ?? "";
                _itemId = NormalizeHandItem(data.item_id);
                _userSource = NormalizeUserSource(data.userSource);
                _botSource = WiredBotSourceUtil.NormalizeBotSource(data.botSource ?? WiredBotSourceUtil.SOURCE_BOT_NAME);
                Delay = data.delay;

                // Fix: si botSource era SOURCE_TRIGGER pero hay nombre, usar SOURCE_BOT_NAME
                if (_botSource == WiredSourceUtil.SOURCE_TRIGGER &&
                    !string.IsNullOrEmpty(_botName))
                    _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }
            else if (wiredData.Contains('\t'))
            {
                // Retrocompatibilidad Java legacy: "delay\titemId\tbotName"
                var tabs = wiredData.Split('\t');
                if (tabs.Length == 3)
                {
                    if (int.TryParse(tabs[0], out int delay)) Delay = delay;
                    _itemId = int.TryParse(tabs[1], out int id) ? NormalizeHandItem(id) : 0;
                    _botName = tabs[2];
                }

                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
                _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            }
            else
            {
                // Retrocompatibilidad C# viejo: "botName;itemId;userSource;botSource"
                var parts = wiredData.Split(';');
                if (parts.Length == 4)
                {
                    _botName = parts[0];
                    _itemId = int.TryParse(parts[1], out int id) ? NormalizeHandItem(id) : 0;
                    _userSource = NormalizeUserSource(int.TryParse(parts[2], out int us) ? us : WiredSourceUtil.SOURCE_TRIGGER);
                    _botSource = WiredBotSourceUtil.NormalizeBotSource(int.TryParse(parts[3], out int bs) ? bs : WiredBotSourceUtil.SOURCE_BOT_NAME);
                }
            }

            StringData = $"{_botName};{_itemId};{_userSource};{_botSource}";
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            _itemId = intCount > 0
                ? NormalizeHandItem(packet.PopInt())
                : 0;
            _userSource = intCount > 1
                ? NormalizeUserSource(packet.PopInt())
                : WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = intCount > 2
                ? WiredBotSourceUtil.NormalizeBotSource(packet.PopInt())
                : WiredBotSourceUtil.SOURCE_BOT_NAME;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            _botName = packet.PopString();
            Delay = packet.PopInt();

            StringData = $"{_botName};{_itemId};{_userSource};{_botSource}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furni seleccionable
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(_botName ?? "");
            packet.WriteInteger(3); // itemId, userSource, botSource
            packet.WriteInteger(_itemId);
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
            if (_itemId <= 0) return false;

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

            // Resolver usuario destino
            RoomUser actor = null;
            if (_pendingActor != null)
                actor = Instance.GetRoomUserManager().GetRoomUserByHabbo(_pendingActor.Id);

            if (actor == null)
            {
                _pendingActor = null;
                return false;
            }

            // Resolver bot
            var bot = ResolveBot();
            if (bot == null)
            {
                _pendingActor = null;
                return false;
            }

            if (Instance.GetGameMap() == null)
            {
                _pendingActor = null;
                return false;
            }

            // El bot ya está ocupado con otro usuario
            if (bot.BotData.TargetUser != 0)
            {
                _pendingActor = null;
                return false;
            }

            // Encontrar tile adyacente al actor para que el bot se acerque
            var targetTile = GetClosestAdjacentTile(bot, actor);
            if (targetTile == null)
            {
                _pendingActor = null;
                return false;
            }

            // Bot lleva el hand item y camina hacia el actor
            bot.CarryItem(_itemId);
            bot.BotData.TargetUser = actor.HabboId;
            bot.MoveTo(targetTile.Value.X, targetTile.Value.Y);

            _pendingActor = null;
            return true;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private RoomUser ResolveBot()
        {
            if (_botSource == WiredBotSourceUtil.SOURCE_BOT_NAME ||
                _botSource == WiredSourceUtil.SOURCE_TRIGGER)
                return Instance.GetRoomUserManager().GetBotByName(_botName);

            // Para otros sources devolver el primer bot de la sala
            foreach (var ru in Instance.GetRoomUserManager().GetRoomUsers())
                if (ru != null && ru.IsBot) return ru;

            return null;
        }

        /// <summary>
        /// Devuelve el tile adyacente al actor más cercano al bot (port de getClosestAdjacentTile).
        /// </summary>
        private Point? GetClosestAdjacentTile(RoomUser bot, RoomUser actor)
        {
            var candidates = new[]
            {
                new Point(actor.X + 1, actor.Y),
                new Point(actor.X - 1, actor.Y),
                new Point(actor.X,     actor.Y + 1),
                new Point(actor.X,     actor.Y - 1)
            };

            Point? best = null;
            double bestD = double.MaxValue;
            var map = Instance.GetGameMap();

            foreach (var p in candidates)
            {
                if (!map.ValidTile(p.X, p.Y)) continue;
                if (map.GetFloorStatus(p) == 0) continue;
                if (map.SquareHasUsers(p.X, p.Y)) continue;

                double d = System.Math.Sqrt(
                    System.Math.Pow(p.X - bot.X, 2) +
                    System.Math.Pow(p.Y - bot.Y, 2));

                if (d < bestD) { bestD = d; best = p; }
            }

            return best;
        }

        private static int NormalizeHandItem(int value) => value < 0 ? 0 : value;

        private static int NormalizeUserSource(int value) =>
            WiredSourceUtil.IsDefaultUserSource(value)
                ? value
                : WiredSourceUtil.SOURCE_TRIGGER;

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string bot_name { get; set; }
            public int item_id { get; set; }
            public int delay { get; set; }
            public int userSource { get; set; }
            public int? botSource { get; set; }
        }
    }
}