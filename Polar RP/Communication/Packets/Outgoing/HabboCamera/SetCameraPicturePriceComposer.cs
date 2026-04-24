using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.HabboCamera
{
    internal class SetCameraPicturePriceComposer : ServerPacket
    {
        public int PriceCredits { get; }
        public int PriceDuckets { get; }
        public int PricePublishDucket { get; }
        public SetCameraPicturePriceComposer(int BuyPicCreditCost, int BuyPicDucketCost, int PublishPicDucketCost)
            : base(ServerPacketHeader.SetCameraPicturePriceMessageComposer)
        {
            this.PriceCredits = BuyPicCreditCost;
            this.PriceDuckets = BuyPicDucketCost;
            this.PricePublishDucket = PublishPicDucketCost;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PriceCredits);
            packet.WriteInteger(PriceDuckets);
            packet.WriteInteger(PricePublishDucket);
        }
    }
}
