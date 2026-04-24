using Polar.HabboHotel.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class ScrSendUserInfoComposer : ServerPacket
    {
        public Habbo habbo { get; }
        public ScrSendUserInfoComposer(Habbo Habbo)
            : base(ServerPacketHeader.ScrSendUserInfoMessageComposer)
        {
            this.habbo = Habbo;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString("habbo_club");

            if (habbo.GetClubManager().HasSubscription("habbo_vip"))
            {

                Double Expire = habbo.GetClubManager().GetSubscription("habbo_vip").ExpireTime;
                Double TimeLeft = Expire - PolarEnvironment.GetUnixTimestamp();
                int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400);
                int MonthsLeft = TotalDaysLeft / 31;

                if (MonthsLeft >= 1)
                {
                    MonthsLeft--;
                }
                packet.WriteInteger(TotalDaysLeft - (MonthsLeft * 31));
                packet.WriteInteger(2); // ??
                packet.WriteInteger(MonthsLeft);
                packet.WriteInteger(1); // type
                packet.WriteBoolean(true);
                packet.WriteBoolean(true);
                packet.WriteInteger(0);
                packet.WriteInteger(Convert.ToInt32(TimeLeft)); // days i have on hc
                packet.WriteInteger(Convert.ToInt32(TimeLeft)); // days i have on vip
            }
            else
            {
                packet.WriteInteger(0);
                packet.WriteInteger(0); // ??
                packet.WriteInteger(0);
                packet.WriteInteger(0); // type
                packet.WriteBoolean(false);
                packet.WriteBoolean(false);
                packet.WriteInteger(0);
                packet.WriteInteger(0); // days i have on hc
                packet.WriteInteger(0); // days i have on vip
            }
        }
    }
}
