using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class PurchaseErrorComposer : ServerPacket
    {
        public int ErrorCode { get; }

        public PurchaseErrorComposer(int ErrorCode)
            : base(ServerPacketHeader.PurchaseErrorMessageComposer)
        {
            this.ErrorCode = ErrorCode;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ErrorCode);
        }
    }
}

