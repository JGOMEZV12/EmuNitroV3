using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Badges;

namespace Polar.Communication.Packets.Outgoing.Inventory.Badges
{
    internal class BadgesComposer : ServerPacket
    {
        public ICollection<Badge> Badges { get; }

        public BadgesComposer(ICollection<Badge> Badges)
            : base(ServerPacketHeader.BadgesMessageComposer)
        {
            this.Badges = Badges;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            List<Badge> EquippedBadges = new List<Badge>();

            packet.WriteInteger(Badges.Count);
            foreach (Badge Badge in Badges)
            {
                packet.WriteInteger(1);
                packet.WriteString(Badge.Code);

                if (Badge.Slot > 0)
                    EquippedBadges.Add(Badge);
            }

            packet.WriteInteger(EquippedBadges.Count);
            foreach (Badge Badge in EquippedBadges)
            {
                packet.WriteInteger(Badge.Slot);
                packet.WriteString(Badge.Code);
            }
        }
    }
}
