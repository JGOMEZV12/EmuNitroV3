using System;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionError : ServerPacket
    {
        public OnGuideSessionError()
            : base(ServerPacketHeader.OnGuideSessionError)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);
        }
    }
}