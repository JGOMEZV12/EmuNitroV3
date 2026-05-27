using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class BotMovesToFurniBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectBotMovesToFurniBox;
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
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
        private bool _requested;

        public BotMovesToFurniBox(Room instance, Item item)
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
                bot_name = StringData ?? "",
                items = SetItems.Keys.ToList(),
                delay = Delay,
                furniSource = _furniSource,
                botSource = _botSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            StringData = "";
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = WiredBotSourceUtil.SOURCE_BOT_NAME;
            Delay = 0;
            SetItems.Clear();

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                StringData = data.bot_name ?? "";
                _furniSource = data.furniSource;
                _botSource = WiredBotSourceUtil.NormalizeBotSource(
                                   data.botSource ?? WiredBotSourceUtil.SOURCE_BOT_NAME);
                Delay = data.delay;

                foreach (int id in data.items ?? new List<int>())
                {
                    var it = Instance.GetRoomItemHandler().GetItem(id);
                    if (it != null)
                        SetItems.TryAdd(it.Id, it);
                }

                if (_furniSource == WiredSourceUtil.SOURCE_TRIGGER && SetItems.Count > 0)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "furniSource;botSource;botName"
                // con items guardados por separado en ItemsData
                var parts = wiredData.Split(';');
                if (parts.Length >= 3)
                {
                    int.TryParse(parts[0], out _furniSource);
                    _botSource = WiredBotSourceUtil.NormalizeBotSource(
                        int.TryParse(parts[1], out int bs) ? bs : WiredBotSourceUtil.SOURCE_BOT_NAME);
                    StringData = parts[2];
                }

                // Cargar ítems desde ItemsData si existe
                if (!string.IsNullOrEmpty(ItemsData))
                {
                    foreach (var s in ItemsData.Split(';'))
                    {
                        if (!int.TryParse(s, out int id)) continue;
                        var it = Instance.GetRoomItemHandler().GetItem(id);
                        if (it != null) SetItems.TryAdd(it.Id, it);
                    }
                }

                if (_furniSource == WiredSourceUtil.SOURCE_TRIGGER && SetItems.Count > 0)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            _furniSource = intCount > 0
                ? packet.PopInt()
                : WiredSourceUtil.SOURCE_TRIGGER;
            _botSource = intCount > 1
                ? WiredBotSourceUtil.NormalizeBotSource(packet.PopInt())
                : WiredBotSourceUtil.SOURCE_BOT_NAME;
            for (int i = 2; i < intCount; i++) packet.PopInt();

            StringData = packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                var selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public void Serialize(ServerPacket packet)
        {
            // Limpiar ítems que ya no están en la sala
            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(2);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_botSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (string.IsNullOrEmpty(StringData)) return false;
            if (SetItems.Count == 0) return false;

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

            var bots = WiredBotSourceUtil.ResolveBots(Instance, _botSource, StringData);
            if (bots == null || bots.Count == 0) return false;

            // Limpiar ítems que ya no están en la sala (igual que Java removeIf)
            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
                SetItems.TryRemove(id, out _);

            var validItems = SetItems.Values
                .Where(i => i != null && Instance.GetRoomItemHandler().GetFloor.Contains(i))
                .ToList();

            if (validItems.Count == 0) return false;
            if (Instance.GetGameMap() == null) return false;

            foreach (var bot in bots)
            {
                if (bot.RoomId != Instance.RoomId) continue;

                // Preferir ítems que el bot no esté pisando ya (port de possibleItems)
                var candidates = validItems
                    .Where(i => !(bot.X == i.GetX && bot.Y == i.GetY))
                    .ToList();

                // Si todos están ocupados por el bot, usar la lista completa
                var pool = candidates.Count > 0 ? candidates : validItems;
                var target = pool[Random.Shared.Next(pool.Count)];

                if (bot.IsWalking)
                    bot.ClearMovement(true);

                bot.MoveTo(target.GetX, target.GetY);
            }

            return true;
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public string bot_name { get; set; }
            public List<int> items { get; set; }
            public int delay { get; set; }
            public int furniSource { get; set; }
            public int? botSource { get; set; }
        }
    }
}