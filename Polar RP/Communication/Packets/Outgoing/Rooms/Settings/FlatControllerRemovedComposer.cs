using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class FlatControllerRemovedComposer : ServerPacket
    {
        public int RoomId { get; }
        public int UserId { get; }
        public FlatControllerRemovedComposer(int RoomId, int UserId)
            : base(ServerPacketHeader.FlatControllerRemovedMessageComposer)
        {
            this.RoomId = RoomId;
            this.UserId = UserId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
            packet.WriteInteger(UserId);
        }
    }
}
