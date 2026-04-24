using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.LandingView
{
    internal class ConcurrentUsersGoalProgressComposer : ServerPacket
    {
        public int UsersNow { get; }
        public ConcurrentUsersGoalProgressComposer(int usersNow)
            : base(ServerPacketHeader.ConcurrentUsersGoalProgressMessageComposer)
        {
            this.UsersNow = usersNow;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);
            packet.WriteInteger(UsersNow);
            packet.WriteInteger(1000);
        }
    }
}
