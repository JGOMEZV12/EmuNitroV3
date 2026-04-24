using System;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class GuideSessionEndedMessageComposer : ServerPacket
    {
        public int id { get; }
        public GuideSessionEndedMessageComposer(int id)
            : base(ServerPacketHeader.GuideSessionEndedMessageComposer)
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