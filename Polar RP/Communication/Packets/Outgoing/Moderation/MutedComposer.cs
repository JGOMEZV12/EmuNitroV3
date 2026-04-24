using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class MutedComposer : ServerPacket
    {
        public double TimeMuted { get; }

        public MutedComposer(double TimeMuted)
            : base(ServerPacketHeader.MutedMessageComposer)
        {
            this.TimeMuted = TimeMuted;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Convert.ToInt32(TimeMuted));
        }
    }
}
