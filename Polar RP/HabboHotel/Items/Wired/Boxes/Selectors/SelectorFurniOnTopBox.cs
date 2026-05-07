using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class SelectorFurniOnTopBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniOnTop;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SelectorFurniOnTopBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
            string data = packet.PopString();
            this.StringData = data;
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

            // Selector usually works on items in SetItems of the trigger or itself?
            // In Arcturus, "On Top" usually means items on top of the items selected in the box.

            var itemsToSearch = SetItems.Values.ToList();
            if (itemsToSearch.Count == 0) return true;

            context.SelectedItems.Clear();
            foreach (var baseItem in itemsToSearch)
            {
                var onTop = Instance.GetRoomItemHandler().GetFloor.Where(i => i.GetX == baseItem.GetX && i.GetY == baseItem.GetY && i.GetZ > baseItem.GetZ);
                context.SelectedItems.AddRange(onTop);
            }

            return true;
        }
    }
}
