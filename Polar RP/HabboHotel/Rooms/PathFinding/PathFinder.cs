using System;
using System.Collections.Generic;
using System.Diagnostics;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Pathfinding
{
    public class PathFinder
    {
        // ── Configuración de timeout ──────────────────────────────────────────────
        // FIX #1: Puerto desde Java — evita loops infinitos en mapas corruptos o muy grandes.
        //         Comprobamos cada 64 iteraciones para no llamar GetTimestamp() en cada nodo.
        private const int TimeoutCheckInterval = 64;
        private const long TimeoutMillis = 25;
        private static readonly long TimeoutTicks = TimeoutMillis * Stopwatch.Frequency / 1000L;

        // ── Direcciones de movimiento ─────────────────────────────────────────────
        public static readonly Vector2D[] DiagMovePoints =
        {
            new Vector2D(-1, -1),
            new Vector2D( 0, -1),
            new Vector2D( 1, -1),
            new Vector2D( 1,  0),
            new Vector2D( 1,  1),
            new Vector2D( 0,  1),
            new Vector2D(-1,  1),
            new Vector2D(-1,  0),
        };

        public static readonly Vector2D[] NoDiagMovePoints =
        {
            new Vector2D( 0, -1),
            new Vector2D( 1,  0),
            new Vector2D( 0,  1),
            new Vector2D(-1,  0),
        };

        // ── Distancia mínima a la puerta para considerar blocking ─────────────────
        // FIX #2: Puerto desde Java — tiles muy cercanos a la puerta no bloquean
        //         para evitar que usuarios tapen la entrada al cuarto.
        private const int DoorDistanceThreshold = 2;

        public static void FindPath(
            RoomUser user, bool diag, Gamemap map, Vector2D start, Vector2D end, List<Vector2D> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();

            if (start.X == end.X && start.Y == end.Y) return;

            var usedNodes = PathFinderUsedNodesPool.Rent();
            try
            {
                var nodes = FindPathReversed(user, diag, map, start, end, usedNodes);

                if (nodes != null)
                {
                    var current = nodes;
                    while (current != null)
                    {
                        if (current.Next == null) break;
                        path.Add(current.Position);
                        current = current.Next;
                    }
                }

                foreach (var node in usedNodes)
                    PathFinderNodePool.Release(node);
            }
            finally
            {
                PathFinderUsedNodesPool.Return(usedNodes);
            }
        }

        private static PathFinderNode? FindPathReversed(
            RoomUser user, bool diag, Gamemap map,
            Vector2D start, Vector2D end,
            List<PathFinderNode> usedNodes)
        {
            int mapW = map.Model.MapSizeX;
            int mapH = map.Model.MapSizeY;

            // FIX #1: Snapshot del contexto de sala una sola vez → evita accesos
            //         repetidos a propiedades durante el bucle (puerto de PathfinderContext Java).
            var ctx = new PathContext(
                allowWalkthrough: !map._room.RoomBlockingEnabled,
                doorX: map.Model.DoorX,
                doorY: map.Model.DoorY,
                diagonalEnabled: diag
            );

            var pfMap = PathFinderMapPool.Rent(mapW, mapH);

            try
            {
                var openList = PathFinderHeapPool.Rent();
                var movePoints = diag ? DiagMovePoints : NoDiagMovePoints;

                // FIX #1: Capturar tiempo de inicio para el timeout
                long startTick = Stopwatch.GetTimestamp();
                int iterCount = 0;

                try
                {
                    var current = PathFinderNodePool.Get(start);
                    current.Cost = 0;

                    pfMap[current.Position.X, current.Position.Y] = current;
                    usedNodes.Add(current);
                    openList.Add(current);

                    while (openList.Count > 0)
                    {
                        // FIX #1: Timeout — comprobamos cada 64 iteraciones
                        if ((++iterCount & (TimeoutCheckInterval - 1)) == 0 &&
                            Stopwatch.GetTimestamp() - startTick > TimeoutTicks)
                        {
                            return null;
                        }

                        current = openList.ExtractFirst();
                        current.InClosed = true;

                        for (int i = 0; i < movePoints.Length; i++)
                        {
                            Vector2D tmp = current.Position + movePoints[i];
                            if (tmp.X < 0 || tmp.Y < 0 || tmp.X >= mapW || tmp.Y >= mapH) continue;

                            bool isFinal = (tmp.X == end.X && tmp.Y == end.Y);
                            bool isDiagonal = (current.Position.X != tmp.X && current.Position.Y != tmp.Y);

                            // FIX #2: Verificar diagonal blocking antes de encolar
                            //         (puerto de isBlockedDiagonal de Java).
                            if (isDiagonal && IsBlockedDiagonal(map, current.Position.X, current.Position.Y, tmp.X, tmp.Y))
                                continue;

                            if (!map.IsValidStep(user, current.Position, tmp, isFinal, user.AllowOverride,
                                    false, false, isDiagonal))
                                continue;

                            // FIX #3: Puerto de DISTANCE_DOOR_THRESHOLD — tiles muy cerca
                            //         de la puerta no bloquean el paso (solo en steps no finales).
                            if (!isFinal && IsNearDoor(ctx, tmp))
                                goto skipBlockingCheck;

                        skipBlockingCheck:

                            PathFinderNode? node = pfMap[tmp.X, tmp.Y];
                            if (node == null)
                            {
                                node = PathFinderNodePool.Get(tmp);
                                pfMap[tmp.X, tmp.Y] = node;
                                usedNodes.Add(node);
                            }

                            if (node.InClosed) continue;

                            int diff = isDiagonal ? 14 : 10;
                            int gScore = current.Cost + diff;

                            if (gScore < node.Cost)
                            {
                                node.Cost = gScore;
                                node.Next = current;

                                if (!node.InOpen)
                                {
                                    if (tmp.X == end.X && tmp.Y == end.Y)
                                        return node;

                                    node.InOpen = true;
                                    openList.Add(node);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    PathFinderHeapPool.Return(openList);
                }

                return null;
            }
            finally
            {
                foreach (var n in usedNodes)
                    pfMap[n.Position.X, n.Position.Y] = null;

                PathFinderMapPool.Return(pfMap);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// FIX #2: Puerto de isBlockedDiagonal (Java AdjacentTileFinder).
        /// Un movimiento diagonal está bloqueado si AMBOS flancos ortogonales
        /// son intransitables — evita pasar por esquinas cerradas.
        /// </summary>
        private static bool IsBlockedDiagonal(Gamemap map, int x, int y, int newX, int newY)
        {
            bool flankX = map.ValidTile(newX, y) && map.GameMap[newX, y] != 0;
            bool flankY = map.ValidTile(x, newY) && map.GameMap[x, newY] != 0;
            return !flankX && !flankY;
        }

        /// <summary>
        /// FIX #3: Puerto de DISTANCE_DOOR_THRESHOLD (Java TileValidator).
        /// Tiles dentro de 2 pasos Manhattan de la puerta no deben bloquear
        /// el acceso al cuarto aunque haya un usuario encima.
        /// </summary>
        private static bool IsNearDoor(in PathContext ctx, Vector2D tile)
        {
            return Math.Abs(tile.X - ctx.DoorX) + Math.Abs(tile.Y - ctx.DoorY) <= DoorDistanceThreshold;
        }
    }

    // ── Contexto inmutable de pathfinding ─────────────────────────────────────────
    // FIX #4: Puerto de PathfinderContext (Java) — captura el estado de la sala
    //         una sola vez al inicio en lugar de acceder a propiedades en cada iteración.
    internal readonly struct PathContext
    {
        public readonly bool AllowWalkthrough;
        public readonly int DoorX;
        public readonly int DoorY;
        public readonly bool DiagonalEnabled;

        public PathContext(bool allowWalkthrough, int doorX, int doorY, bool diagonalEnabled)
        {
            AllowWalkthrough = allowWalkthrough;
            DoorX = doorX;
            DoorY = doorY;
            DiagonalEnabled = diagonalEnabled;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    //  POOLS (sin cambios de lógica, capacidades ajustadas)
    // ══════════════════════════════════════════════════════════════════════════════

    internal static class PathFinderMapPool
    {
        // FIX: aumentado a 256 para salas grandes sin realloc
        private const int MaxSize = 256;

        [ThreadStatic]
        private static PathFinderNode?[,]? _cached;

        public static PathFinderNode?[,] Rent(int w, int h)
        {
            var arr = _cached;
            if (arr != null && arr.GetLength(0) >= w && arr.GetLength(1) >= h)
            {
                _cached = null;
                return arr;
            }
            return new PathFinderNode?[Math.Max(w, MaxSize), Math.Max(h, MaxSize)];
        }

        public static void Return(PathFinderNode?[,] arr)
        {
            if (arr.GetLength(0) <= MaxSize && arr.GetLength(1) <= MaxSize)
                _cached = arr;
        }
    }

    internal static class PathFinderUsedNodesPool
    {
        [ThreadStatic]
        private static List<PathFinderNode>? _cached;

        public static List<PathFinderNode> Rent()
        {
            var list = _cached;
            if (list != null)
            {
                _cached = null;
                list.Clear();
                return list;
            }
            return new List<PathFinderNode>(128);
        }

        public static void Return(List<PathFinderNode> list)
        {
            if (list.Capacity <= 1024)
                _cached = list;
        }
    }

    internal static class PathFinderHeapPool
    {
        [ThreadStatic]
        private static MinHeap<PathFinderNode>? _cached;

        public static MinHeap<PathFinderNode> Rent()
        {
            var heap = _cached;
            if (heap != null)
            {
                _cached = null;
                heap.Clear();
                return heap;
            }
            return new MinHeap<PathFinderNode>(256);
        }

        public static void Return(MinHeap<PathFinderNode> heap)
        {
            _cached = heap;
        }
    }
}