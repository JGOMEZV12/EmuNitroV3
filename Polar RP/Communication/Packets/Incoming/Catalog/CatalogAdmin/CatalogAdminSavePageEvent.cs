using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminSavePageEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    pageId      = packet.PopInt();
            string caption     = packet.PopString();
            string caption2    = packet.PopString();
            string layout      = packet.PopString();
            int    iconType    = packet.PopInt();
            int    minRank     = packet.PopInt();
            bool   visible     = packet.PopBoolean();
            bool   enabled     = packet.PopBoolean();
            int    orderNum    = packet.PopInt();
            int    parentId    = packet.PopInt();
            string headline    = packet.PopString();
            string teaser      = packet.PopString();
            string textDetails = packet.PopString();
            string pageTypeStr = packet.PopString();
            string catalogMode = packet.PopString();

            bool isBuilder = pageTypeStr?.ToUpper() == "BUILDER";

            CatalogPage page = PolarEnvironment.GetGame().GetCatalog().GetCatalogPage(pageId, pageTypeStr);

            if (page == null)
            {
                session.SendMessage(new CatalogAdminResultComposer(false, $"Page not found: {pageId}"));
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (isBuilder)
                {
                    dbClient.SetQuery(
                        "UPDATE `catalog_pages_bc` SET `caption` = @caption, `page_layout` = @layout, " +
                        "`icon_image` = @iconType, `visible` = @visible, `enabled` = @enabled, " +
                        "`order_num` = @orderNum, `parent_id` = @parentId, `page_headline` = @headline, " +
                        "`page_teaser` = @teaser, `page_text_details` = @textDetails WHERE `id` = @id");
                }
                else
                {
                    dbClient.SetQuery(
                        "UPDATE `catalog_pages` SET `caption` = @caption, `caption_save` = @caption2, " +
                        "`page_layout` = @layout, `icon_image` = @iconType, `min_rank` = @minRank, " +
                        "`visible` = @visible, `enabled` = @enabled, `order_num` = @orderNum, " +
                        "`parent_id` = @parentId, `page_headline` = @headline, `page_teaser` = @teaser, " +
                        "`page_text_details` = @textDetails, `catalog_mode` = @catalogMode WHERE `id` = @id");
                    dbClient.AddParameter("caption2",    caption2 ?? "");
                    dbClient.AddParameter("minRank",     minRank);
                    dbClient.AddParameter("catalogMode", catalogMode ?? "");
                }

                dbClient.AddParameter("caption",     caption ?? "");
                dbClient.AddParameter("layout",      layout ?? "");
                dbClient.AddParameter("iconType",    iconType);
                dbClient.AddParameter("visible",     visible ? "1" : "0");
                dbClient.AddParameter("enabled",     enabled ? "1" : "0");
                dbClient.AddParameter("orderNum",    orderNum);
                dbClient.AddParameter("parentId",    parentId);
                dbClient.AddParameter("headline",    headline ?? "");
                dbClient.AddParameter("teaser",      teaser ?? "");
                dbClient.AddParameter("textDetails", textDetails ?? "");
                dbClient.AddParameter("id",          pageId);

                dbClient.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Page saved"));
        }
    }
}
