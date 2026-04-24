using System;

namespace Polar.HabboHotel.Pathfinding
{
    public readonly struct Vector2D : IEquatable<Vector2D>
    {
        public static readonly Vector2D Zero = new Vector2D(0, 0);

        public readonly int X;
        public readonly int Y;

        public Vector2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int GetDistanceSquared(Vector2D point)
        {
            int dx = X - point.X;
            int dy = Y - point.Y;
            return dx * dx + dy * dy;
        }

        public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

        public bool Equals(Vector2D other) => X == other.X && Y == other.Y;

        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }

        public override string ToString() => $"{X}, {Y}";

        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);
        public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);
    }
}