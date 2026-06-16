using Polar.Communication.Packets.Outgoing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;

// FIX: Eliminados using duplicados de Packets.Incoming y Rooms

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class FurniHasFurniBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.ConditionFurniHasFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public FurniHasFurniBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
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
        public bool Execute(params object[] @params)
        {
            foreach (Item item in SetItems.Values.ToList())
            {
                if (item == null || !Instance.GetRoomItemHandler().GetFloor.Contains(item))
                    continue;

                bool hasFurni = false;
                List<Item> items = Instance.GetGameMap().GetAllRoomItemForSquare(item.GetX, item.GetY);
                if (items.Count(x => x.GetZ >= item.GetZ) > 1)
                    hasFurni = true;

                if (!hasFurni)
                    return false;
            }

            return true;
        }
    }
}
