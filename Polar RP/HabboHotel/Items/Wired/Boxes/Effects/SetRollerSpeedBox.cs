using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class SetRollerSpeedBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectSetRollerSpeed; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public SetRollerSpeedBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            // FIX: Eliminado el Clear() inútil — el diccionario se acaba de crear, siempre estará vacío
            this.SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket Packet)
        {
            if (this.SetItems.Count > 0)
                this.SetItems.Clear();

            int Unknown = Packet.PopInt();
            string Message = Packet.PopString();

            this.StringData = Message;

            int Speed;
            if (!int.TryParse(StringData, out Speed))
            {
                this.StringData = "";
            }
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
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
        }
        public bool Execute(params object[] Params)
        {
            int Speed;
            if (int.TryParse(this.StringData, out Speed))
            {
                Instance.GetRoomItemHandler().SetSpeed(Speed);
            }
            return true;
        }
    }
}
