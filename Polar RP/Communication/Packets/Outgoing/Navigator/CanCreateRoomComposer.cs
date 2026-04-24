using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class CanCreateRoomComposer : ServerPacket
    {
        public bool Error { get; }
        public int MaxRoomsPerUser { get; }

        public CanCreateRoomComposer(bool error, int maxRoomsPerUser)
            : base(ServerPacketHeader.CanCreateRoomMessageComposer)
        {
            this.Error = error;
            this.MaxRoomsPerUser = maxRoomsPerUser;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Error ? 1 : 0);
            packet.WriteInteger(MaxRoomsPerUser);
        }
    }
}
