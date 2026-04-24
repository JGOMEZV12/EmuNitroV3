using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListNotificationComposer : ServerPacket
    {

        public FurniListNotificationComposer(int id, int type)
            : base(ServerPacketHeader.FurniListNotificationMessageComposer)
        {
            base.WriteInteger(1);
            base.WriteInteger(type);
            base.WriteInteger(1);
            base.WriteInteger(id);
        }

        public FurniListNotificationComposer(List<Item> items, int type)
            : base(ServerPacketHeader.FurniListNotificationMessageComposer)
        {
            base.WriteInteger(1);
            base.WriteInteger(type);
            base.WriteInteger(items.Count);
            foreach (Item i in items)
            {
                base.WriteInteger(i.Id);
            }
        }
    }
}