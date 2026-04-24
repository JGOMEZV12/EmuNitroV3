using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminSavePageIconEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int pageId = packet.PopInt();
            int iconId = packet.PopInt();

            CatalogPage page = PolarEnvironment.GetGame().GetCatalog().GetCatalogPage(pageId);

            if (page == null)
            {
                session.SendMessage(new CatalogAdminResultComposer(false, $"Page not found: {pageId}"));
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `catalog_pages` SET `icon_image` = @iconId WHERE `id` = @id");
                dbClient.AddParameter("iconId", iconId);
                dbClient.AddParameter("id",     pageId);
                dbClient.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Page icon saved"));
        }
    }
}
