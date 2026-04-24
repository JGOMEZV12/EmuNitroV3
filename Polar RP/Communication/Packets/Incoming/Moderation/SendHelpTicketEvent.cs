using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Action;

namespace Polar.Communication.Packets.Incoming.Moderation
{
    internal class SendHelpTicketEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int UserId = Packet.PopInt();

            // Ignore the user who was just reported
            if (PolarEnvironment.GetHabboById(UserId) != null)
                Session.SendMessage(new IgnoreStatusComposer(1, PolarEnvironment.GetHabboById(UserId).Username));
        }
    }
}