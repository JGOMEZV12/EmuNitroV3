using System;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Pathfinding
{
    public class PathFinder
    {
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

        public static void FindPath(
            RoomUser user, bool diag, Gamemap map, Vector2D start, Vector2D end, List<Vector2D> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();

            // Optimization: If start == end, return empty path immediately
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
                        // Skip the start node (it's the last one in the chain)
                        if (current.Next == null) break;

                        path.Add(current.Position);
                        current = current.Next;
                    }
                }

                // Now it is safe to release all nodes back to the pool
                foreach (var node in usedNodes)
                {
                    PathFinderNodePool.Release(node);
                }
            }
            finally
            {
                PathFinderUsedNodesPool.Return(usedNodes);
            }
        }

        private static PathFinderNode? FindPathReversed(
            RoomUser user, bool diag, Gamemap map, Vector2D start, Vector2D end, List<PathFinderNode> usedNodes)
        {
            int mapW = map.Model.MapSizeX;
            int mapH = map.Model.MapSizeY;

            var pfMap = PathFinderMapPool.Rent(mapW, mapH);

            try
            {
                var openList = PathFinderHeapPool.Rent();
                var movePoints = diag ? DiagMovePoints : NoDiagMovePoints;

                try
                {
                    var current = PathFinderNodePool.Get(start);
                    current.Cost = 0;

                    pfMap[current.Position.X, current.Position.Y] = current;
                    usedNodes.Add(current);
                    openList.Add(current);

                    while (openList.Count > 0)
                    {
                        current = openList.ExtractFirst();
                        current.InClosed = true;

                        for (int i = 0; i < movePoints.Length; i++)
                        {
                            Vector2D tmp = current.Position + movePoints[i];
                            if (tmp.X < 0 || tmp.Y < 0 || tmp.X >= mapW || tmp.Y >= mapH) continue;

                            bool isFinal = (tmp.X == end.X && tmp.Y == end.Y);
                            bool isDiagonal = (current.Position.X != tmp.X && current.Position.Y != tmp.Y);

                            if (!map.IsValidStep(user, current.Position, tmp, isFinal, user.AllowOverride, false, false, isDiagonal))
                                continue;

                            PathFinderNode? node = pfMap[tmp.X, tmp.Y];
                            if (node == null)
                            {
                                node = PathFinderNodePool.Get(tmp);
                                pfMap[tmp.X, tmp.Y] = node;
                                usedNodes.Add(node);
                            }

                            if (node.InClosed) continue;

                            int diff = (current.Position.X != tmp.X && current.Position.Y != tmp.Y) ? 14 : 10;
                            int gScore = current.Cost + diff;

                            if (gScore < node.Cost)
                            {
                                node.Cost = gScore;
                                node.Next = current;

                                if (!node.InOpen)
                                {
                                    if (tmp.X == end.X && tmp.Y == end.Y)
                                    {
                                        return node;
                                    }
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
    }

    internal static class PathFinderMapPool
    {
        private const int MaxSize = 128;

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