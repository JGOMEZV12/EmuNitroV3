using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class SelectorFurniByTypeBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniByType;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SelectorFurniByTypeBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            string data = packet.PopString();
            this.StringData = data; // Usually contains SpriteId
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            if (@params.Length == 0 || !(@params[0] is WiredContext context))
                return false;

            if (!int.TryParse(StringData, out int spriteId))
                return true;

            context.SelectedItems.Clear();
            var allItems = Instance.GetRoomItemHandler().GetFloor.Concat(Instance.GetRoomItemHandler().GetWall);

            foreach (var item in allItems)
            {
                if (item.GetBaseItem().SpriteId == spriteId)
                    context.SelectedItems.Add(item);
            }

            return true;
        }
    }
}
