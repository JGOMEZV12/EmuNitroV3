using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Support;
using Polar.Utilities;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorSupportTicketComposer : ServerPacket
    {
        public SupportTicket Ticket { get; }

        public ModeratorSupportTicketComposer(SupportTicket Ticket)
          : base(ServerPacketHeader.ModeratorSupportTicketMessageComposer)
        {
            this.Ticket = Ticket;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Ticket.Id);
            packet.WriteInteger(Ticket.TabId);
            packet.WriteInteger(Ticket.Type); // Type
            packet.WriteInteger(Ticket.Category); // Category
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - (int)Ticket.Timestamp) * 1000);
            packet.WriteInteger(Ticket.Score);
            packet.WriteInteger(0);
            packet.WriteInteger(Ticket.SenderId);
            packet.WriteString(Ticket.SenderName);
            packet.WriteInteger(Ticket.ReportedId);
            packet.WriteString(Ticket.ReportedName);
            packet.WriteInteger((Ticket.Status == TicketStatus.PICKED) ? Ticket.ModeratorId : 0);
            packet.WriteString(Ticket.ModName);
            packet.WriteString(Ticket.Message);
            packet.WriteInteger(0);//No idea?
            packet.WriteInteger(0);//String, int, int - this is the "matched to" a string
            {
                packet.WriteString("fresh-hotel.org");
                packet.WriteInteger(-1);
                packet.WriteInteger(-1);
            }
        }
    }
}