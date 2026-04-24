using System;

namespace Polar.HabboHotel.Rooms
{
    public enum SquareState
    {
        OPEN = 0,
        BLOCKED = 1,
        SEAT = 2,
        POOL = 3,
        VIP = 4
    }

    public class RoomModel
    {
        // ─────────────────────────────────────
        //  Propiedades (antes campos públicos mutables)
        // ─────────────────────────────────────
        public int DoorOrientation { get; private set; }
        public int DoorX { get; private set; }
        public int DoorY { get; private set; }
        public double DoorZ { get; private set; }
        public int WallHeight { get; private set; }
        public int MapSizeX { get; private set; }
        public int MapSizeY { get; private set; }
        public string Heightmap { get; private set; }
        public bool GotPublicPool { get; private set; }

        public short[,] SqFloorHeight { get; private set; }
        public byte[,] SqSeatRot { get; private set; }
        public SquareState[,] SqState { get; private set; }
        public byte[,] RoomModelFx { get; private set; }

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public RoomModel(string id, int doorX, int doorY, double doorZ, int doorOrientation,
            string heightmap, int wallHeight, string poolmap)
        {
            if (string.IsNullOrEmpty(heightmap))
                throw new ArgumentException("Heightmap cannot be null or empty.", nameof(heightmap));

            DoorX = doorX;
            DoorY = doorY;
            DoorZ = doorZ;
            DoorOrientation = doorOrientation;
            WallHeight = wallHeight;

            // FIX: guardamos el heightmap ya en minúsculas
            Heightmap = heightmap.ToLower();
            GotPublicPool = !string.IsNullOrEmpty(poolmap);

            // FIX: Convert.ToChar(13) → '\r' más legible
            // FIX: Split con StringSplitOptions para ignorar líneas vacías al final
            string[] tmpHeightmap = Heightmap.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // FIX: poolmap null-safe — si no hay pool usamos array vacío en lugar de llamar Split sobre null
            string[] tmpFxMap = GotPublicPool
                ? poolmap.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            MapSizeX = tmpHeightmap[0].Length;
            MapSizeY = tmpHeightmap.Length;

            SqState = new SquareState[MapSizeX, MapSizeY];
            SqFloorHeight = new short[MapSizeX, MapSizeY];
            SqSeatRot = new byte[MapSizeX, MapSizeY];

            if (GotPublicPool)
                RoomModelFx = new byte[MapSizeX, MapSizeY];

            // FIX: catch vacío eliminado — si el parse falla ahora se propaga correctamente
            // con el id del modelo en el mensaje para facilitar el debug
            try
            {
                for (int y = 0; y < MapSizeY; y++)
                {
                    // FIX: Replace doble (\r y \n) reemplazado por el Split con ambos separadores arriba
                    string line = tmpHeightmap[y];

                    for (int x = 0; x < line.Length; x++)
                    {
                        char square = line[x];
                        if (square == 'x')
                        {
                            SqState[x, y] = SquareState.BLOCKED;
                        }
                        else
                        {
                            SqState[x, y] = SquareState.OPEN;
                            SqFloorHeight[x, y] = Parse(square);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to parse RoomModel '{id}': {e.Message}", e);
            }
        }

        // ─────────────────────────────────────
        //  Parse — FIX: switch de 36 cases reemplazado por aritmética
        // ─────────────────────────────────────

        /// <summary>
        /// Convierte un carácter de heightmap (0-9, a-z) en su valor numérico (0-35).
        /// </summary>
        public static short Parse(char input)
        {
            if (input >= '0' && input <= '9')
                return (short)(input - '0');           // 0–9

            if (input >= 'a' && input <= 'z')
                return (short)(input - 'a' + 10);      // 10–35

            throw new FormatException($"Invalid heightmap character '{input}'. Must be 0-9 or a-z.");
        }

        /// <summary>
        /// Convierte un carácter numérico (0-9) en byte.
        /// </summary>
        public static byte ParseByte(char input)
        {
            if (input >= '0' && input <= '9')
                return (byte)(input - '0');

            throw new FormatException($"Invalid byte character '{input}'. Must be 0-9.");
        }

        // ─────────────────────────────────────
        //  Destroy
        // ─────────────────────────────────────
        public void Destroy()
        {
            Heightmap = null;
            SqState = null;
            SqFloorHeight = null;
            SqSeatRot = null;
            RoomModelFx = null;
        }
    }
}