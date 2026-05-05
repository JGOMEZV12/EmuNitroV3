using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    /// <summary>
    /// Equivalente al Java RoomHeightMapComposer.
    /// Envía: boolean (true) + wallHeight (int) + heightmap string (normalizado a \r).
    /// Usa el heightmap ESTÁTICO de DB, no el reconstruido desde arrays.
    /// </summary>
    internal class FloorHeightMapComposer : ServerPacket
    {
        public FloorHeightMapComposer(Room room)
            : base(ServerPacketHeader.FloorHeightMapMessageComposer)
        {
            WriteBoolean(true);
            WriteInteger(room.GetGameMap().StaticModel.WallHeight);

            // Java: room.getLayout().getRelativeMap()
            //   = this.heightmap.replace("\r\n", "\r")
            // Usa el heightmap ESTÁTICO original de DB, normalizado a \r
            string relativeMap = room.GetGameMap().StaticModel.Heightmap
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");

            WriteString(relativeMap);
        }
    }
}