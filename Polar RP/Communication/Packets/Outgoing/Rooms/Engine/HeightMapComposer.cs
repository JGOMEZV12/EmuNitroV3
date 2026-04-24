using System;
using System.Linq;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class HeightMapComposer : ServerPacket
    {
        private const char INVALID_TILE = 'x';
        private const int HEIGHT_MULTIPLIER = 256;
        private const short INVALID_HEIGHT = -1;

        public HeightMapComposer(string Map)
            : base(ServerPacketHeader.HeightMapMessageComposer)
        {
            if (string.IsNullOrWhiteSpace(Map))
                throw new ArgumentException("El mapa de alturas no puede estar vacío.", nameof(Map));

            // Fix: Nitro V3 requires consistent parsing of all line ending types (\r\n, \r, \n)
            string[] rows = Map.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (rows.Length == 0)
                throw new InvalidOperationException("No se encontraron filas en el mapa de alturas.");

            int width = rows[0].Length;
            int totalTiles = width * rows.Length;

            base.WriteInteger(width);
            base.WriteInteger(totalTiles);

            for (int y = 0; y < rows.Length; y++)
            {
                string currentRow = rows[y];
                int rowWidth = currentRow.Length;

                for (int x = 0; x < width; x++)
                {
                    char tileChar = (x < rowWidth) ? currentRow[x] : INVALID_TILE;
                    short heightValue = GetHeightValue(tileChar);
                    base.WriteShort(heightValue);
                }
            }
        }

        private short GetHeightValue(char c)
        {
            if (c == INVALID_TILE)
                return INVALID_HEIGHT;

            if (char.IsDigit(c))
            {
                int digit = c - '0';
                return (short)(digit * HEIGHT_MULTIPLIER);
            }

            if (c >= 'a' && c <= 'z')
            {
                int value = (c - 'a') + 10;
                return (short)(value * HEIGHT_MULTIPLIER);
            }

            return INVALID_HEIGHT;
        }
    }
}
