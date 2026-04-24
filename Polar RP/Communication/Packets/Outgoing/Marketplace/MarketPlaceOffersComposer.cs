using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Catalog.Marketplace;

namespace Polar.Communication.Packets.Outgoing.Marketplace
{
    internal class MarketPlaceOffersComposer : ServerPacket
    {
        public Dictionary<int, MarketOffer> Dictionary { get; }
        public Dictionary<int, int> Dictionary2 { get; }
        public int minCost { get; }
        public int maxCost { get; }
        public MarketPlaceOffersComposer(int MinCost, int MaxCost, Dictionary<int, MarketOffer> dictionary, Dictionary<int, int> dictionary2)
            : base(ServerPacketHeader.MarketPlaceOffersMessageComposer)
        {
            this.Dictionary = dictionary;
            this.Dictionary2 = dictionary2;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Dictionary.Count);
            if (Dictionary.Count > 0)
            {
                foreach (KeyValuePair<int, MarketOffer> pair in Dictionary)
                {
                    packet.WriteInteger(pair.Value.OfferID);
                    packet.WriteInteger(1);//State
                    packet.WriteInteger(1);
                    packet.WriteInteger(pair.Value.SpriteId);

                    packet.WriteInteger(256);
                    packet.WriteString("");
                    packet.WriteInteger(pair.Value.LimitedNumber);
                    packet.WriteInteger(pair.Value.LimitedStack);

                    packet.WriteInteger(pair.Value.TotalPrice);
                    packet.WriteInteger(0);
                    packet.WriteInteger(PolarEnvironment.GetGame().GetCatalog().GetMarketplace().AvgPriceForSprite(pair.Value.SpriteId));
                    packet.WriteInteger(Dictionary2[pair.Value.SpriteId]);
                }
            }
            packet.WriteInteger(Dictionary.Count);//Item count to show how many were found.
        }
    }
}
