using Polar.Core;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.Catalog.Utilities;
using Polar.HabboHotel.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    class CatalogAdminResultComposer : ServerPacket
    {
        public CatalogAdminResultComposer(bool success, string message)
            : base(ServerPacketHeader.CatalogAdminResultComposer)
        {
            //Logging.WriteLine($"Creando CatalogOfferComposer para item: {Item.Id}, Tipo: {Item.Data.Type}");
            base.WriteBoolean(success);
            base.WriteString(message);
        }
    }
}