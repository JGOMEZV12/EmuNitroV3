using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class FurniTypeMatchesBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.ConditionFurniTypeMatches; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public FurniTypeMatchesBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
            this.BoolData = false;
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            string Unknown2 = Packet.PopString();

            int FurniCount = Packet.PopInt();
            SetItems.Clear();
            for (int i = 0; i < FurniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(Packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(5);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item item in SetItems.Values.ToList())
                Packet.WriteInteger(item.Id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }

        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0)
                return false;

            Habbo Player = Params[0] as Habbo;
            if (Player == null)
                return false;

            RoomUser User = Instance.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null)
                return false;

            Item itemTriggered = null;
            if (Params.Length > 1)
                itemTriggered = Params[1] as Item;

            if (itemTriggered == null)
                return false;

            foreach (Item item in SetItems.Values)
            {
                if (item.GetBaseItem().Id == itemTriggered.GetBaseItem().Id)
                    return true;
            }

            return false;
        }
    }
}
