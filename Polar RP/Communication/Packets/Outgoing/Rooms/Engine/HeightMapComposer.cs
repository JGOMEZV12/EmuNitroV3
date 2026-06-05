using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    /// <summary>
    /// Equivalente al Java RoomRelativeMapComposer.
    /// El cliente (RoomHeightMapParser) decodifica así:
    ///   isRoomTile  = height >= 0        → negativo = tile inválido/bloqueado
    ///   tileHeight  = (height & 16383) / 256
    /// short.MaxValue (32767) es POSITIVO → cliente lo trataría como tile válido
    /// con altura ~127, causando suelo visible donde no debería haberlo.
    /// Solución: usar -1 para tiles bloqueados.
    /// </summary>
    internal class HeightMapComposer : ServerPacket
    {
        private const short BLOCKED_TILE = -1;

        public HeightMapComposer(Room room)
            : base(ServerPacketHeader.HeightMapMessageComposer)
        {
            var layout = room.GetGameMap();
            int mapSizeX = layout.Model.MapSizeX;
            int mapSizeY = layout.Model.MapSizeY;

            WriteInteger(mapSizeX);
            WriteInteger(mapSizeX * mapSizeY);

            for (short y = 0; y < mapSizeY; y++)
            {
                for (short x = 0; x < mapSizeX; x++)
                {
                    if (!layout.ValidTile(x, y) ||
                        layout.Model.SqState[x, y] == SquareState.BLOCKED)
                    {
                        WriteShort(BLOCKED_TILE); // negativo → isRoomTile() = false
                        continue;
                    }

                    // (absoluteHeight * 256) → cliente decodifica: (value & 16383) / 256
                    WriteShort((short)(layout.SqAbsoluteHeight(x, y) * 256));
                }
            }
        }
    }
}