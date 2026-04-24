using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Bots;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Badges;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class HabboUserBadgesComposer : ServerPacket
    {
        public Habbo Habbo { get; }
        public RoleplayBot Bot { get; }
        public HabboUserBadgesComposer(Habbo Habbo, RoleplayBot Bot = null)
            : base(ServerPacketHeader.HabboUserBadgesMessageComposer)
        {
            this.Habbo = Habbo;
            this.Bot = Bot;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (Bot != null)
            {
                packet.WriteInteger(Bot.Id + 1000000);
                packet.WriteInteger(1);
                packet.WriteInteger(1);
                packet.WriteString("BOT");
            }
            else
            {
                packet.WriteInteger(Habbo.Id);
                packet.WriteInteger(Habbo.GetBadgeComponent().EquippedCount);

                foreach (Badge Badge in Habbo.GetBadgeComponent().GetBadges().ToList())
                {
                    if (Badge.Slot <= 0)
                        continue;

                    packet.WriteInteger(Badge.Slot);
                    packet.WriteString(Badge.Code);
                }
            }
        }
    }
}
