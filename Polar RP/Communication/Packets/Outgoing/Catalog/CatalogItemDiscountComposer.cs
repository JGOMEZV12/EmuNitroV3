using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class CatalogItemDiscountComposer : ServerPacket
    {
        public CatalogItemDiscountComposer()
            : base(ServerPacketHeader.CatalogItemDiscountMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(100);//Most you can get.
            packet.WriteInteger(6);
            packet.WriteInteger(1);
            packet.WriteInteger(1);
            packet.WriteInteger(2);//Count
            {
                packet.WriteInteger(40);
                packet.WriteInteger(99);
            }
        }
    }
}