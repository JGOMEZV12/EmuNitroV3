using System;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionDetachedComposer : ServerPacket
    {
        public int id { get; }
        public OnGuideSessionDetachedComposer(int id)
            : base(ServerPacketHeader.OnGuideSessionDetachedComposer)
        {
            this.id = id;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (id == 0 || id == 2)
                packet.WriteInteger(id);
        }
    }
}