using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Marketplace
{
    internal class MarketplaceItemStatsComposer : ServerPacket
    {
        public int ItemId { get; }
        public int SpriteId { get; }
        public int AveragePrice { get; }

        public MarketplaceItemStatsComposer(int ItemId, int SpriteId, int AveragePrice)
            : base(ServerPacketHeader.MarketplaceItemStatsMessageComposer)
        {
            this.ItemId = ItemId;
            this.SpriteId = SpriteId;
            this.AveragePrice = AveragePrice;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(AveragePrice);//Avg price in last 7 days.
            packet.WriteInteger(PolarEnvironment.GetGame().GetCatalog().GetMarketplace().OfferCountForSprite(SpriteId));

            packet.WriteInteger(0);//No idea.
            packet.WriteInteger(0);//No idea.

            packet.WriteInteger(ItemId);
            packet.WriteInteger(SpriteId);
        }
    }
}