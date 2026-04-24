using System.Drawing;

// ✅ FIX #4: Existían DOS archivos definiendo el tipo ThreeDCoord en NAMESPACES DISTINTOS:
//   • ThreeDCoord.cs → namespace Polar.HabboHotel.Rooms.Pathfinding
//   • Coord.cs       → namespace Polar.HabboHotel.Pathfinding
//
//   Esto fuerza a todos los callers a elegir qué namespace usar y provoca ambigüedad
//   en archivos que tienen using de ambos namespaces (e.g. GameMap.cs, PathFinder.cs).
//   Consolidado aquí en el namespace canónico (Rooms.Pathfinding).
//   → Coord.cs debe eliminarse del proyecto.

namespace Polar.HabboHotel.Rooms.Pathfinding
{
    public struct ThreeDCoord : IEquatable<ThreeDCoord>
    {
        public int X;
        public int Y;
        public int Z;

        public ThreeDCoord(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static bool operator ==(ThreeDCoord a, ThreeDCoord b) =>
            a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        public static bool operator !=(ThreeDCoord a, ThreeDCoord b) => !(a == b);

        public bool Equals(ThreeDCoord other) =>
            X == other.X && Y == other.Y && Z == other.Z;

        public bool Equals(Point other) =>
            X == other.X && Y == other.Y;

        // ✅ FIX #5: Antes: X ^ Y ^ Z  (XOR puro)
        //   XOR entre enteros produce colisiones masivas para coordenadas de sala:
        //     (1,0,0), (0,1,0) y (0,0,1) producen hash = 1 los tres.
        //     (a,b,c) colisiona con (a^b^c, 0, 0) sistemáticamente.
        //   Ahora: combinación multiplicativa de Bernstein — distribución uniforme,
        //   sin colisiones para las coordenadas típicas de un mapa de Habbo.
        public override int GetHashCode()
        {
            unchecked
            {
                int h = X;
                h = h * 397 ^ Y;
                h = h * 397 ^ Z;
                return h;
            }
        }

        // ✅ FIX #6: Antes: base.GetHashCode().Equals(obj.GetHashCode())
        //   Comparar HASHES no es lo mismo que comparar VALORES:
        //     • Falso positivo: dos objetos distintos con hash colisionado → "iguales".
        //     • Falso negativo: mismas coordenadas, hash distinto por bug en GetHashCode
        //       anterior → "distintos".
        //   Es un bug de correctitud que rompe cualquier colección basada en igualdad
        //   (Dictionary, HashSet, ConcurrentDictionary) que use ThreeDCoord como clave.
        public override bool Equals(object obj)
        {
            if (obj is ThreeDCoord other)
                return Equals(other);
            return false;
        }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
