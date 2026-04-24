using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class UserCountDoesntInRoomBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.ConditionUserCountDoesntInRoom; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public UserCountDoesntInRoomBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new();
        }

        public void HandleSave(ClientPacket Packet)
        {
            int Unknown = Packet.PopInt();
            int CountOne = Packet.PopInt();
            int CountTwo = Packet.PopInt();

            this.StringData = CountOne + ";" + CountTwo;
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);
            if (String.IsNullOrEmpty(StringData)) StringData = "0;0";
            Packet.WriteInteger(2);
            Packet.WriteInteger(int.Parse(StringData.Split(';')[0]));
            Packet.WriteInteger(int.Parse(StringData.Split(';')[1]));
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] Params)
        {
            if (Params.Length == 0)
                return false;

            if (string.IsNullOrEmpty(this.StringData))
                return false;

            // FIX: int.Parse reemplazado por TryParse para evitar FormatException
            int CountOne = 1, CountTwo = 50;
            var parts = this.StringData.Split(';');
            if (parts.Length >= 2)
            {
                int.TryParse(parts[0], out CountOne);
                int.TryParse(parts[1], out CountTwo);
            }

            if (Instance.UserCount >= CountOne && Instance.UserCount <= CountTwo)
                return false;

            return true;
        }
    }
}
