using Polar.HabboHotel.LandingView;
using System.Collections.Generic;
using Polar.HabboHotel.Cache;
using System;

namespace Polar.Communication.Packets.Outgoing.LandingView
{
    internal class HallOfFameComposer : ServerPacket
    {
        public HallOfFameComposer(HallOfFame List) : base(ServerPacketHeader.UpdateHallOfFameListMessageComposer)
        {
            base.WriteString("");

            int index = 0;
            foreach (var user in List.StaffList)
            {

                this.WriteInteger(user.id); // userId
                this.WriteString(user.username); // userName
                this.WriteString(user.look); // figure
                this.WriteInteger(index); // rank
                this.WriteInteger(user.level); // currentScore

                index++;
            }
        }
    }
}
