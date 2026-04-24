using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Handshake
{
    internal class PongComposer : ServerPacket
    {
        public PongComposer()
            : base(ServerPacketHeader.PongMessageComposer) {}
    }
}
