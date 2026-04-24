namespace Polar.HabboHotel.Pathfinding
{
    // ✅ FIX #3: Era "sealed internal" — internal lo ocultaba a otros ensamblados
    //            sin razón (se usa desde Rooms). Cambiado a public sealed.
    public sealed class Vector3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Z { get; set; }

        public Vector3D() { }

        public Vector3D(int x, int y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector2D ToVector2D() => new Vector2D(X, Y);
    }
}
