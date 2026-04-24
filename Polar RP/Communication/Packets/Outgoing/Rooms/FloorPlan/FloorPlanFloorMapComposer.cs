using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.FloorPlan
{
    internal class FloorPlanFloorMapComposer : ServerPacket
    {
        public List<Point> Items { get; }

        public FloorPlanFloorMapComposer(List<Point> Items)
            : base(ServerPacketHeader.FloorPlanFloorMapMessageComposer)
        {
            this.Items = Items;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Items.Count);//TODO: Figure this out, it pushes the room coords, but it iterates them, x,y|x,y|x,y|and so on.
            foreach (Point Item in Items.ToList())
            {
                packet.WriteInteger(Item.X);
                packet.WriteInteger(Item.Y);
            }
        }
    }
}
