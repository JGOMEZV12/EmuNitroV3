using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.RentableSpaces;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.RentableSpaces
{
    internal class GetRentableSpaceEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int ItemId = Packet.PopInt();
            var Room = Session.GetHabbo().CurrentRoom;
            Item Item = null;

            if (Room != null)
                Item = Room.GetRoomItemHandler().GetItem(ItemId);

            Session.SendMessage(new RentableSpaceComposer(Item, Session));
        }
    }
}
