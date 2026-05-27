using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ToggleFurniBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        private const int TOGGLE_TYPE_NEXT = 0;
        private const int TOGGLE_TYPE_PREVIOUS = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectToggleFurniState;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public int TickCount { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }

        private int _delay = 0;
        private long _next = 0;
        private bool _requested = false;
        private int _toggleType = TOGGLE_TYPE_NEXT;
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;

        public ToggleFurniBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            if (intCount > 0) _toggleType = NormalizeToggleType(packet.PopInt());
            if (intCount > 1) _furniSource = packet.PopInt();
            for (int i = 2; i < intCount; i++) packet.PopInt();

            packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null && !Instance.GetWired().OtherBoxHasItem(this, selected.Id))
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                toggleType = _toggleType,
                furniSource = _furniSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _toggleType = TOGGLE_TYPE_NEXT;
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Delay = data.delay;
                _toggleType = NormalizeToggleType(data.toggleType);
                _furniSource = data.furniSource;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "delay\tid1;id2;"
                var parts = wiredData.Split('\t');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int delay))
                    Delay = delay;

                if (parts.Length == 2 && parts[1].Contains(";"))
                {
                    foreach (var s in parts[1].Split(';'))
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        if (!int.TryParse(s, out int id)) continue;

                        var item = Instance.GetRoomItemHandler().GetItem(id);
                        if (item != null)
                            SetItems.TryAdd(item.Id, item);
                    }
                }

                _toggleType = TOGGLE_TYPE_NEXT;
                _furniSource = SetItems.Count == 0
                    ? WiredSourceUtil.SOURCE_TRIGGER
                    : WiredSourceUtil.SOURCE_SELECTED;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        public void Serialize(ServerPacket packet)
        {
            var toRemove = SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(2);
            packet.WriteInteger(_toggleType);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (_next == 0 || _next < PolarEnvironment.Now())
                _next = PolarEnvironment.Now() + Delay;

            _requested = true;
            TickCount = Delay;
            return true;
        }

        public bool OnCycle()
        {
            if (SetItems.Count == 0 || !_requested)
                return false;

            if (_next < PolarEnvironment.Now())
            {
                foreach (Item item in SetItems.Values.ToList())
                {
                    if (item == null) continue;

                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                    {
                        SetItems.TryRemove(item.Id, out _);
                        continue;
                    }

                    ToggleItemState(item);
                }

                _requested = false;
                _next = 0;
                TickCount = Delay;
            }

            return true;
        }

        private void ToggleItemState(Item item)
        {
            try
            {
                int stateCount = item.GetBaseItem().Modes;
                if (stateCount <= 1) return;

                int currentState = 0;
                if (!string.IsNullOrEmpty(item.ExtraData))
                {
                    if (!int.TryParse(item.ExtraData, out currentState))
                    {
                        item.Interactor.OnWiredTrigger(item);
                        return;
                    }
                }

                int nextState = (_toggleType == TOGGLE_TYPE_PREVIOUS)
                    ? ((currentState - 1 + stateCount) % stateCount)
                    : ((currentState + 1) % stateCount);

                if (currentState == nextState) return;

                item.ExtraData = nextState.ToString();
                item.UpdateNeeded = true;
                Instance.GetRoomItemHandler().UpdateItem(item);      // DB
                Instance.GetGameMap().UpdateMapForItem(item);         // mapa/walkable
                item.UpdateState(false, true);                        // packet al cliente
            }
            catch (Exception ex)
            {
                Polar.Core.Logging.LogException("[ToggleFurniBox] " + ex);
            }
        }

        private int NormalizeToggleType(int value) =>
            value == TOGGLE_TYPE_PREVIOUS ? TOGGLE_TYPE_PREVIOUS : TOGGLE_TYPE_NEXT;

        private class JsonData
        {
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int toggleType { get; set; }
            public int furniSource { get; set; }
        }
    }
}