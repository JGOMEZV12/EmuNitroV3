using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminDeleteOfferEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    offerId     = packet.PopInt();
            string pageTypeStr = packet.PopString();
            bool   isBuilder   = pageTypeStr?.ToUpper() == "BUILDER";

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(isBuilder
                    ? "DELETE FROM `catalog_items_bc` WHERE `id` = @id"
                    : "DELETE FROM `catalog_items` WHERE `id` = @id");
                dbClient.AddParameter("id", offerId);
                dbClient.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Offer deleted"));
        }
    }
}
