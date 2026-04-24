using System;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class GuideSessionRequesterRoomMessageComposer : ServerPacket
    {
        public int id { get; }
        public GuideSessionRequesterRoomMessageComposer(int id)
            : base(ServerPacketHeader.GuideSessionRequesterRoomMessageComposer)
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