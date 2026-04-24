using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.FloorPlan
{
    internal class FloorPlanSendDoorComposer : ServerPacket
    {
        public int DoorX { get; }
        public int DoorY { get; }
        public int DoorDirection { get; }

        public FloorPlanSendDoorComposer(int DoorX, int DoorY, int DoorDirection)
            : base(ServerPacketHeader.FloorPlanSendDoorMessageComposer)
        {
            this.DoorX = DoorX;
            this.DoorY = DoorY;
            this.DoorDirection = DoorDirection;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(DoorX);
            packet.WriteInteger(DoorY);
            packet.WriteInteger(DoorDirection);
        }
    }
}
