namespace Polar.HabboHotel.Rooms.Pathfinding
{
    public static class Rotation
    {
        public static int Calculate(int x1, int y1, int x2, int y2)
        {
            // ✅ FIX #13: Reemplazada la cadena de if/else con una lookup table
            //   basada en el signo de (dx, dy). Mismo resultado, cero branches,
            //   más legible y compilable como jump table por el JIT.
            int dx = x2 - x1;
            int dy = y2 - y1;

            if (dx == 0 && dy == 0) return 0;

            int sx = Math.Sign(dx); //  -1, 0, +1
            int sy = Math.Sign(dy); //  -1, 0, +1

            return (sx, sy) switch
            {
                ( 0, -1) => 0, // norte
                ( 1, -1) => 1, // noreste
                ( 1,  0) => 2, // este
                ( 1,  1) => 3, // sureste
                ( 0,  1) => 4, // sur
                (-1,  1) => 5, // suroeste
                (-1,  0) => 6, // oeste
                (-1, -1) => 7, // noroeste
                _        => 0
            };
        }

        public static int Calculate(int x1, int y1, int x2, int y2, bool moonwalk)
        {
            int rot = Calculate(x1, y1, x2, y2);
            return moonwalk ? RotationInverse(rot) : rot;
        }

        // ✅ FIX #14: Typo "RotationIverse" → "RotationInverse".
        //   Mantenemos el nombre antiguo como alias obsoleto por compatibilidad binaria.
        public static int RotationInverse(int rot) => rot > 3 ? rot - 4 : rot + 4;

        [Obsolete("Use RotationInverse (typo fixed). This overload will be removed in a future version.")]
        public static int RotationIverse(int rot) => RotationInverse(rot);
    }
}
