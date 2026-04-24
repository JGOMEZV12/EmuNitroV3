using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class RoomInfoUpdatedComposer : ServerPacket
    {
        public int RoomId { get; }

        public RoomInfoUpdatedComposer(int roomId)
            : base(ServerPacketHeader.RoomInfoUpdatedMessageComposer)
        {
            this.RoomId = roomId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
        }
    }
}
