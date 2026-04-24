using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Support;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.Communication.Packets.Incoming.Moderation
{
    internal class GetModeratorTicketChatlogsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().GetPermissions().HasRight("mod_tickets"))
                return;

            SupportTicket Ticket = PolarEnvironment.GetGame().GetModerationTool().GetTicket(Packet.PopInt());
            if (Ticket == null)
                return;

            RoomData Data = PolarEnvironment.GetGame().GetRoomManager().GenerateRoomData(Ticket.RoomId);
            if (Data == null)
                return;

            Session.SendMessage(new ModeratorTicketChatlogComposer(Ticket, Data, Ticket.Timestamp));
        }
    }
}
