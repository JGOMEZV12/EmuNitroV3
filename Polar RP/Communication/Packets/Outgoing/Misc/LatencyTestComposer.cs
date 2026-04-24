using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Misc
{
    internal class LatencyTestComposer : ServerPacket
    {
        public int testResponce { get; }
        public LatencyTestComposer(int testResponce)
            : base(ServerPacketHeader.LatencyResponseMessageComposer)
        {
            this.testResponce = testResponce;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {

            packet.WriteInteger(testResponce);
        }
    }
}
