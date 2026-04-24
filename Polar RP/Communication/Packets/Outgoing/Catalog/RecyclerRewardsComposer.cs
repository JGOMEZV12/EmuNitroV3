using System;
using System.Collections.Generic;

using Polar.HabboHotel.Catalog;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class RecyclerRewardsComposer : ServerPacket
    {
        public RecyclerRewardsComposer()
            : base(ServerPacketHeader.RecyclerRewardsMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);// Count of items
        }
    }
}