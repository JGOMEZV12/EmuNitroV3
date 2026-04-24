using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class GetUserTagsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int UserId = Packet.PopInt();

            Session.SendMessage(new UserTagsComposer(UserId, Session));
        }
    }
}
