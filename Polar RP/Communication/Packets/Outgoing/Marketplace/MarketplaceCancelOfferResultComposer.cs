using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Marketplace
{
    internal class MarketplaceCancelOfferResultComposer : ServerPacket
    {
        public int OfferId { get; }
        public bool Success { get; }

        public MarketplaceCancelOfferResultComposer(int OfferId, bool Success)
            : base(ServerPacketHeader.MarketplaceCancelOfferResultMessageComposer)
        {
            this.OfferId = OfferId;
            this.Success = Success;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(OfferId);
            packet.WriteBoolean(Success);
        }
    }
}
