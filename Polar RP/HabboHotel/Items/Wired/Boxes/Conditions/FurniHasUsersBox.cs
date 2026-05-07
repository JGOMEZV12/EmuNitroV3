using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class FurniHasUsersBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.ConditionFurniHasUsers;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private bool all = false;
        private int furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

        public FurniHasUsersBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            // 1. Int params
            int paramsCount = packet.PopInt();
            bool rawAll = paramsCount > 0 && packet.PopInt() == 1;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;

            // 2. String param
            string strParam = packet.PopString();

            // 3. Furnis
            int furniCount = packet.PopInt();
            Console.WriteLine($"[FurniHasUsersBox] HandleSave — all={rawAll}, furniSource={rawSource}");
            
            this.all = rawAll;
            this.furniSource = rawSource;
            this.SetItems.Clear();

            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            if (SetItems.Count > 0 && furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            Refresh();
            return JsonConvert.SerializeObject(new JsonData
            {
                itemIds = SetItems.Keys.ToList(),
                furniSource = this.furniSource,
                all = this.all
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            this.all = false;
            this.furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.all = data.all;
                this.furniSource = data.furniSource;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }
            }
            else
            {
                // Retrocompatibilidad: formato viejo "?:id1;id2;"
                var parts = wiredData.Split(':');
                if (parts.Length >= 2)
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

                this.all = false;
                this.furniSource = SetItems.Count == 0
                    ? WiredBoxTypeUtility.SOURCE_TRIGGER
                    : WiredBoxTypeUtility.SOURCE_SELECTED;
            }

            if (furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER && SetItems.Count > 0)
                furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;

            ItemsData = string.Join(";", SetItems.Keys);
        }

        public void Serialize(ServerPacket packet)
        {
            Refresh();

            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(2);
            packet.WriteInteger(this.all ? 1 : 0);
            packet.WriteInteger(this.furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Refresh();

            var targets = SetItems.Values.ToList();
            if (targets.Count == 0) return true;

            if (this.all)
                return targets.All(item => HasUsersOnItem(item));
            else
                return targets.Any(item => HasUsersOnItem(item));
        }

        private bool HasUsersOnItem(Item item)
        {
            if (item == null || !Instance.GetRoomItemHandler().GetFloor.Contains(item))
                return false;

            if (Instance.GetGameMap().SquareHasUsers(item.GetX, item.GetY))
                return true;

            foreach (Point tile in item.GetCoords)
            {
                if (Instance.GetGameMap().SquareHasUsers(tile.X, tile.Y))
                    return true;
            }

            return false;
        }

        private void Refresh()
        {
            var toRemove = SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                SetItems.TryRemove(id, out _);
        }

        private class JsonData
        {
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
            public bool all { get; set; }
        }
    }
}