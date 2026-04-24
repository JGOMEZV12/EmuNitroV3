using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminDeletePageEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    pageId      = packet.PopInt();
            string pageTypeStr = packet.PopString();
            bool   isBuilder   = pageTypeStr?.ToUpper() == "BUILDER";

            CatalogPage page = PolarEnvironment.GetGame().GetCatalog().GetCatalogPage(pageId, pageTypeStr);

            if (page == null)
            {
                session.SendMessage(new CatalogAdminResultComposer(false, $"Page not found: {pageId}"));
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(isBuilder
                    ? "DELETE FROM `catalog_pages_bc` WHERE `id` = @id"
                    : "DELETE FROM `catalog_pages` WHERE `id` = @id");
                dbClient.AddParameter("id", pageId);
                dbClient.RunQuery();
            }

            PolarEnvironment.GetGame().GetCatalog().RemoveCatalogPage(pageId, pageTypeStr);

            session.SendMessage(new CatalogAdminResultComposer(true, "Page deleted"));
        }
    }
}
