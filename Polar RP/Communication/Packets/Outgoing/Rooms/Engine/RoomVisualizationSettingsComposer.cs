using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class RoomVisualizationSettingsComposer : ServerPacket
    {
        public int Walls { get; }
        public int Floor { get; }
        public bool HideWalls { get; }

        public RoomVisualizationSettingsComposer(int walls, int floor, bool hideWalls)
            : base(ServerPacketHeader.RoomVisualizationSettingsMessageComposer)
        {
            this.Walls = walls;
            this.Floor = floor;
            this.HideWalls = hideWalls;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(HideWalls);
            packet.WriteInteger(Walls);
            packet.WriteInteger(Floor);
        }
    }
}