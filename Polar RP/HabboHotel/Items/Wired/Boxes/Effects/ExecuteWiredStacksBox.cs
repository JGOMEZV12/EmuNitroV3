using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ExecuteWiredStacksBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.EffectExecuteWiredStacks;
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

        private int _delay = 0;
        protected int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private readonly Queue<Habbo> _queue = new Queue<Habbo>();

        public ExecuteWiredStacksBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
        }

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            _furniSource = intCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            for (int i = 1; i < intCount; i++) packet.PopInt();

            packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Delay = data.delay;
                _furniSource = data.furniSource;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                    _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: "delay\tid1;id2;"
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

                _furniSource = SetItems.Count == 0
                    ? WiredBoxTypeUtility.SOURCE_TRIGGER
                    : WiredBoxTypeUtility.SOURCE_SELECTED;
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
            packet.WriteInteger(1);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo player = Params.Length > 0 ? Params[0] as Habbo : null;
            if (player == null) return false;

            _queue.Enqueue(player);
            TickCount = Delay;
            return true;
        }

        public bool OnCycle()
        {
            if (_queue.Count == 0)
            {
                TickCount = Delay;
                return true;
            }

            while (_queue.Count > 0)
            {
                Habbo player = _queue.Dequeue();
                if (player == null || player.CurrentRoom != Instance) continue;

                ExecuteWiredStacks(player);
            }

            TickCount = Delay;
            return true;
        }

        protected virtual void ExecuteWiredStacks(Habbo player)
        {
            foreach (Item item in SetItems.Values.ToList())
            {
                if (item == null || !Instance.GetRoomItemHandler().GetFloor.Contains(item) || !item.IsWired)
                    continue;

                if (!Instance.GetWired().TryGet(item.Id, out IWiredItem wiredItem))
                    continue;

                if (wiredItem.Type == WiredBoxType.EffectExecuteWiredStacks)
                    continue;

                foreach (IWiredItem effect in Instance.GetWired().GetEffects(wiredItem).ToList())
                {
                    if (effect.Type == WiredBoxType.EffectExecuteWiredStacks) continue;
                    if (SetItems.ContainsKey(effect.Item.Id) && effect.Item.Id != item.Id) continue;

                    effect.Execute(player);
                }
            }
        }

        private class JsonData
        {
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
        }
    }
}