using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Moderation;
using Polar.HabboHotel.Support;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class ModeratorInitComposer : ServerPacket
    {
        public ICollection<string> UserPresets { get; }
        public ICollection<string> RoomPresets { get; }
        public ICollection<SupportTicket> Tickets { get; }
        public Dictionary<string, List<ModerationPresetActionMessages>> UserActionPresets { get; }
        public ModeratorInitComposer(ICollection<string> UserPresets, ICollection<string> RoomPresets, Dictionary<string, List<ModerationPresetActionMessages>> UserActionPresets, ICollection<SupportTicket> Tickets)
            : base(ServerPacketHeader.ModeratorInitMessageComposer)
        {
            this.UserPresets = UserPresets;
            this.RoomPresets = RoomPresets;
            this.UserActionPresets = UserActionPresets;
            this.Tickets = Tickets;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Tickets.Count);
            foreach (SupportTicket ticket in Tickets.ToList())
            {
                packet.WriteInteger(ticket.Id);
                packet.WriteInteger(ticket.TabId);
                packet.WriteInteger(1); // Type
                packet.WriteInteger(114); // Category
                packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - Convert.ToInt32(ticket.Timestamp)) * 1000);
                packet.WriteInteger(ticket.Score);
                packet.WriteInteger(0);
                packet.WriteInteger(ticket.SenderId);
                packet.WriteString(ticket.SenderName);
                packet.WriteInteger(ticket.ReportedId);
                packet.WriteString(ticket.ReportedName);
                packet.WriteInteger((ticket.Status == TicketStatus.PICKED) ? ticket.ModeratorId : 0);
                packet.WriteString(ticket.ModName);
                packet.WriteString(ticket.Message);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
            }

            packet.WriteInteger(UserPresets.Count);
            foreach (string pre in UserPresets)
            {
                packet.WriteString(pre);
            }

            packet.WriteInteger(0);
            {
                //Push a string, maybe locale for the new shit?
            }

            /*packet.WriteInteger(UserActionPresets.Count);
            foreach (KeyValuePair<string, List<ModerationPresetActionMessages>> Cat in UserActionPresets.ToList())
            {
               packet.WriteString(Cat.Key);
                packet.WriteBoolean(true);
                packet.WriteInteger(Cat.Value.Count);
                foreach (ModerationPresetActionMessages Preset in Cat.Value.ToList())
                {
                   packet.WriteString(Preset.Caption);
                   packet.WriteString(Preset.MessageText);
                    packet.WriteInteger(Preset.BanTime); // Account Ban Hours
                    packet.WriteInteger(Preset.IPBanTime); // IP Ban Hours
                    packet.WriteInteger(Preset.MuteTime); // Mute in Hours
                    packet.WriteInteger(0);//Trading lock duration
                   packet.WriteString(Preset.Notice + "\n\nPlease Note: Avatar ban is an IP ban!");
                    packet.WriteBoolean(false);//Show HabboWay
                }
            }*/

            packet.WriteBoolean(true); // Ticket right
            packet.WriteBoolean(true); // Chatlogs
            packet.WriteBoolean(true); // User actions alert etc
            packet.WriteBoolean(true); // Kick users
            packet.WriteBoolean(true); // Ban users
            packet.WriteBoolean(true); // Caution etc
            packet.WriteBoolean(true); // Love you, Tom

            packet.WriteInteger(RoomPresets.Count);
            foreach (string pre in RoomPresets)
            {
                packet.WriteString(pre);
            }
        }
    }
}