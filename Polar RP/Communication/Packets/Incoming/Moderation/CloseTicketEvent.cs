using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Moderation
{
    internal class CloseTicketEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                return;

            int Result = Packet.PopInt(); // result, 1 = useless, 2 = abusive, 3 = resolved
            int Junk = Packet.PopInt(); // ? 
            int TicketId = Packet.PopInt(); // id

            PolarEnvironment.GetGame().GetModerationTool().CloseTicket(Session, TicketId, Result);
        }
    }
}