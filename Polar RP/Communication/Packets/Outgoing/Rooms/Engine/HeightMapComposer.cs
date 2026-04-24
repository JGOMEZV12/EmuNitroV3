using System;
using System.Linq;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class HeightMapComposer : ServerPacket
    {
        // Constantes para mejorar legibilidad
        private const char INVALID_TILE = 'x';
        private const int HEIGHT_MULTIPLIER = 256;
        private const short INVALID_HEIGHT = -1;

        public HeightMapComposer(string Map)
            : base(ServerPacketHeader.HeightMapMessageComposer)
        {
            if (string.IsNullOrWhiteSpace(Map))
                throw new ArgumentException("El mapa de alturas no puede estar vacío.", nameof(Map));

            // Limpiar y separar filas (asume que las filas terminan con '\r')
            Map = Map.Replace("\n", "");          // Eliminar saltos de línea sobrantes
            string[] rows = Map.Split('\r', StringSplitOptions.RemoveEmptyEntries);

            if (rows.Length == 0)
                throw new InvalidOperationException("No se encontraron filas en el mapa de alturas.");

            int width = rows[0].Length;
            int totalTiles = width * rows.Length;

            // Escribir cabeceras del paquete
            base.WriteInteger(width);          // Ancho del mapa
            base.WriteInteger(totalTiles);     // Total de celdas

            // Recorrer cada fila y columna
            for (int y = 0; y < rows.Length; y++)
            {
                string currentRow = rows[y];
                // Si la fila actual tiene ancho distinto, se completa con 'x' (tile inválido)
                int rowWidth = currentRow.Length;

                for (int x = 0; x < width; x++)
                {
                    char tileChar = (x < rowWidth) ? currentRow[x] : INVALID_TILE;
                    short heightValue = GetHeightValue(tileChar);
                    base.WriteShort(heightValue);
                }
            }
        }

        /// <summary>
        /// Convierte un carácter de altura en el valor numérico a enviar.
        /// </summary>
        private short GetHeightValue(char c)
        {
            if (c == INVALID_TILE)
                return INVALID_HEIGHT;

            // Si es dígito ('0'..'9')
            if (char.IsDigit(c))
            {
                int digit = c - '0';
                return (short)(digit * HEIGHT_MULTIPLIER);
            }

            // Para letras minúsculas ('a'..'z') que representan alturas 10..35
            if (c >= 'a' && c <= 'z')
            {
                int value = (c - 'a') + 10;   // 'a'=10, 'b'=11, ...
                return (short)(value * HEIGHT_MULTIPLIER);
            }

            // Si el carácter no es válido, se considera tile inválido (o se puede loguear)
            // Podrías lanzar una excepción o simplemente devolver -1.
            return INVALID_HEIGHT;
        }
    }
}