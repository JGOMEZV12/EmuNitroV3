using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Support;
using Polar.Utilities;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorTicketChatlogComposer : ServerPacket
    {
        public SupportTicket Ticket { get; }
        public RoomData RoomData { get; }
        public double Timestamp { get; }
        public ModeratorTicketChatlogComposer(SupportTicket ticket, RoomData roomData, double timestamp)
            : base(ServerPacketHeader.ModeratorTicketChatlogMessageComposer)
        {
            this.Ticket = ticket;
            this.RoomData = roomData;
            this.Timestamp = timestamp;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Ticket.TicketId);
            packet.WriteInteger(Ticket.SenderId);
            packet.WriteInteger(Ticket.ReportedId);
            packet.WriteInteger(RoomData.Id);

            packet.WriteByte(1);
            packet.WriteShort(2);//Count
           packet.WriteString("roomName");
            packet.WriteByte(2);
           packet.WriteString(RoomData.Name);
           packet.WriteString("roomId");
            packet.WriteByte(1);
            packet.WriteInteger(RoomData.Id);

            packet.WriteShort( (Ticket == null ) ? 0 : ((Ticket.ReportedChats == null ) ? 0 : Ticket.ReportedChats.Count) );
            if (Ticket.ReportedChats == null)
            {
                Habbo Habbo = PolarEnvironment.GetHabboById(Ticket.ReportedId);
                packet.WriteString(UnixTimestamp.FromUnixTimestamp(Convert.ToInt32(Timestamp)).ToShortTimeString());
                //  packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - Convert.ToInt32(Timestamp)) * 1000);
                packet.WriteInteger(Ticket.ReportedId);
                packet.WriteString(Habbo != null ? Habbo.Username : "No username");
                packet.WriteString("");
                packet.WriteBoolean(false);
                return;
            }

            foreach (string Chat in Ticket.ReportedChats)
            {
                Habbo Habbo = PolarEnvironment.GetHabboById(Ticket.ReportedId);
                packet.WriteString(UnixTimestamp.FromUnixTimestamp(Convert.ToInt32(Timestamp)).ToShortTimeString());
              //  packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - Convert.ToInt32(Timestamp)) * 1000);
                packet.WriteInteger(Ticket.ReportedId);
               packet.WriteString(Habbo != null ? Habbo.Username : "No username");
               packet.WriteString(Chat);
                packet.WriteBoolean(false);
            }
        }
    }
}
