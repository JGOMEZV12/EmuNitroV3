using System;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Pathfinding
{
    public sealed class PathFinderNode : IComparable<PathFinderNode>
    {
        public Vector2D Position;
        public PathFinderNode? Next;
        public int Cost = int.MaxValue;
        public bool InOpen = false;
        public bool InClosed = false;

        public PathFinderNode(Vector2D position)
        {
            Position = position;
        }

        public void Reset(Vector2D position)
        {
            Position = position;
            Next = null;
            Cost = int.MaxValue;
            InOpen = false;
            InClosed = false;
        }

        public int CompareTo(PathFinderNode? other)
        {
            if (other == null) return 1;
            return Cost.CompareTo(other.Cost);
        }

        public override bool Equals(object? obj) =>
            obj is PathFinderNode n && n.Position.Equals(Position);

        public bool Equals(PathFinderNode? node) =>
            node != null && node.Position.Equals(Position);

        public override int GetHashCode() => Position.GetHashCode();
    }

    internal static class PathFinderNodePool
    {
        private static readonly Stack<PathFinderNode> _pool = new Stack<PathFinderNode>(1024);

        public static PathFinderNode Get(Vector2D position)
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                {
                    var node = _pool.Pop();
                    node.Reset(position);
                    return node;
                }
            }
            return new PathFinderNode(position);
        }

        public static void Release(PathFinderNode node)
        {
            lock (_pool)
            {
                if (_pool.Count < 2048) // Limit pool size
                {
                    _pool.Push(node);
                }
            }
        }
    }
}