using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class PresentDeliverErrorMessageComposer : ServerPacket
    {
        public bool CreditError { get; }
        public bool DucketError { get; }

        public PresentDeliverErrorMessageComposer(bool CreditError, bool DucketError)
            : base(ServerPacketHeader.PresentDeliverErrorMessageComposer)
        {
            this.CreditError = CreditError;
            this.DucketError = DucketError;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(CreditError);//Do we have enough credits?
            packet.WriteBoolean(DucketError);//Do we have enough duckets?
        }
    }
}
