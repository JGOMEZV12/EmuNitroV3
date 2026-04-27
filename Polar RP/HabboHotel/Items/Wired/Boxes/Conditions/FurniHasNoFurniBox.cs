using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class FurniHasNoFurniBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.ConditionFurniHasNoFurni; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public FurniHasNoFurniBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new();
        }

                        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();
            this.StringData = packet.PopString();
            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }
            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }

        
                                        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0);

            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(0); // Delay or Selection
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] Params)
        {
            foreach (Item Item in this.SetItems.Values.ToList())
            {
                if (Item == null || !Instance.GetRoomItemHandler().GetFloor.Contains(Item))
                    continue;

                bool NoFurni = false;
                List<Item> Items = Instance.GetGameMap().GetAllRoomItemForSquare(Item.GetX, Item.GetY);
                if (Items.Count == 0)
                    NoFurni = true;

                if (!NoFurni)
                    return false;
            }

            return true;
        }
    }
}
