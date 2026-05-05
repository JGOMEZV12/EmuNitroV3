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
        //  Propiedades
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

            // FIX: ya NO se fuerza ToLower() — Parse() acepta a-z y A-Z
            Heightmap = heightmap;
            GotPublicPool = !string.IsNullOrEmpty(poolmap);

            string[] tmpHeightmap = Heightmap.Split(
                new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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

            try
            {
                for (int y = 0; y < MapSizeY; y++)
                {
                    string line = tmpHeightmap[y];

                    for (int x = 0; x < line.Length; x++)
                    {
                        char square = line[x];

                        if (square == 'x' || square == 'X')
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
        //  Parse
        //  FIX: ahora acepta 0-9, a-z Y A-Z
        //       — consistente con DynamicRoomModel que emite A-Z
        // ─────────────────────────────────────

        /// <summary>
        /// Convierte un carácter de heightmap en su valor numérico (0-35).
        ///   '0'-'9' → 0-9
        ///   'a'-'z' → 10-35  (minúsculas, desde DB)
        ///   'A'-'Z' → 10-35  (mayúsculas, desde DynamicRoomModel)
        /// </summary>
        public static short Parse(char input)
        {
            if (input >= '0' && input <= '9')
                return (short)(input - '0');

            if (input >= 'a' && input <= 'z')
                return (short)(input - 'a' + 10);

            if (input >= 'A' && input <= 'Z')
                return (short)(input - 'A' + 10);

            throw new FormatException(
                $"Invalid heightmap character '{input}'. Must be 0-9, a-z or A-Z.");
        }

        /// <summary>
        /// Convierte un carácter numérico (0-9) en byte.
        /// </summary>
        public static byte ParseByte(char input)
        {
            if (input >= '0' && input <= '9')
                return (byte)(input - '0');

            throw new FormatException(
                $"Invalid byte character '{input}'. Must be 0-9.");
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