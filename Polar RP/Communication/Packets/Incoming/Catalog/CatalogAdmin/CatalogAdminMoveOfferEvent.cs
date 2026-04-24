using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminMoveOfferEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    offerId     = packet.PopInt();
            int    orderNumber = packet.PopInt();
            string pageTypeStr = packet.PopString();
            bool   isBuilder   = pageTypeStr?.ToUpper() == "BUILDER";

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(isBuilder
                    ? "UPDATE `catalog_items_bc` SET `order_number` = @order WHERE `id` = @id"
                    : "UPDATE `catalog_items` SET `order_number` = @order WHERE `id` = @id");
                dbClient.AddParameter("order", orderNumber);
                dbClient.AddParameter("id",    offerId);
                dbClient.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Offer reordered"));
        }
    }
}
