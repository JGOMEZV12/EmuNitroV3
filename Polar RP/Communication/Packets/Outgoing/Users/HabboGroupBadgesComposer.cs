using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class HabboGroupBadgesComposer : ServerPacket
    {
        public Dictionary<int, string> Badges { get; }
        public Group Group { get; }
        public HabboGroupBadgesComposer(Dictionary<int, string> badges)
            : base(ServerPacketHeader.HabboGroupBadgesMessageComposer)
        {
            this.Badges = badges;
            Compose(this);
        }

        public HabboGroupBadgesComposer(Group group)
            : base(ServerPacketHeader.HabboGroupBadgesMessageComposer)
        {
            this.Group = group;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (this.Badges != null)
            {
                packet.WriteInteger(Badges.Count);
                foreach (KeyValuePair<int, string> badge in Badges)
                {
                    packet.WriteInteger(badge.Key);
                    packet.WriteString(badge.Value);
                }
            }
            else if (Group != null)
            {
                packet.WriteInteger(1); //count

                packet.WriteInteger(Group.Id);
                packet.WriteString(Group.Badge);
            }
            else
            {
                packet.WriteInteger(0);
            }
        }
    }
}
