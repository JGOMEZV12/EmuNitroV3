using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Marketplace
{
    internal class MarketplaceCanMakeOfferResultComposer : ServerPacket
    {
        public int Result { get; }

        public MarketplaceCanMakeOfferResultComposer(int Result)
            : base(ServerPacketHeader.MarketplaceCanMakeOfferResultMessageComposer)
        {
            this.Result = Result;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Result);
            packet.WriteInteger(0);
        }
    }
}
