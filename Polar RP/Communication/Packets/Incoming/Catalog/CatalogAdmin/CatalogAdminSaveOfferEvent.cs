using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminSaveOfferEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    offerId      = packet.PopInt();
            int    pageId       = packet.PopInt();
            string itemIds      = packet.PopString();
            string catalogName  = packet.PopString();
            int    costCredits  = packet.PopInt();
            int    costPoints   = packet.PopInt();
            int    pointsType   = packet.PopInt();
            int    amount       = packet.PopInt();
            int    clubOnly     = packet.PopInt();
            string extradata    = packet.PopString();
            bool   haveOffer    = packet.PopBoolean();
            int    offerIdGroup = packet.PopInt();
            int    limitedStack = packet.PopInt();
            int    orderNumber  = packet.PopInt();
            string pageTypeStr  = packet.PopString();

            bool isBuilder      = pageTypeStr?.ToUpper() == "BUILDER";
            bool updateItemIds  = !string.IsNullOrWhiteSpace(itemIds);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (isBuilder)
                {
                    if (updateItemIds)
                    {
                        dbClient.SetQuery(
                            "UPDATE `catalog_items_bc` SET `page_id` = @pageId, `item_ids` = @itemIds, " +
                            "`catalog_name` = @catalogName, `order_number` = @orderNumber, `extradata` = @extradata " +
                            "WHERE `id` = @id");
                        dbClient.AddParameter("itemIds", itemIds.Trim());
                    }
                    else
                    {
                        dbClient.SetQuery(
                            "UPDATE `catalog_items_bc` SET `page_id` = @pageId, `catalog_name` = @catalogName, " +
                            "`order_number` = @orderNumber, `extradata` = @extradata WHERE `id` = @id");
                    }

                    dbClient.AddParameter("pageId",      pageId);
                    dbClient.AddParameter("catalogName", catalogName);
                    dbClient.AddParameter("orderNumber", orderNumber);
                    dbClient.AddParameter("extradata",   extradata ?? "");
                    dbClient.AddParameter("id",          offerId);
                }
                else
                {
                    if (updateItemIds)
                    {
                        dbClient.SetQuery(
                            "UPDATE `catalog_items` SET `page_id` = @pageId, `item_ids` = @itemIds, " +
                            "`catalog_name` = @catalogName, `cost_credits` = @costCredits, `cost_points` = @costPoints, " +
                            "`points_type` = @pointsType, `amount` = @amount, `club_only` = @clubOnly, " +
                            "`extradata` = @extradata, `have_offer` = @haveOffer, `offer_id` = @offerIdGroup, " +
                            "`limited_stack` = @limitedStack, `order_number` = @orderNumber WHERE `id` = @id");
                        dbClient.AddParameter("itemIds", itemIds.Trim());
                    }
                    else
                    {
                        dbClient.SetQuery(
                            "UPDATE `catalog_items` SET `page_id` = @pageId, `catalog_name` = @catalogName, " +
                            "`cost_credits` = @costCredits, `cost_points` = @costPoints, `points_type` = @pointsType, " +
                            "`amount` = @amount, `club_only` = @clubOnly, `extradata` = @extradata, " +
                            "`have_offer` = @haveOffer, `offer_id` = @offerIdGroup, `limited_stack` = @limitedStack, " +
                            "`order_number` = @orderNumber WHERE `id` = @id");
                    }

                    dbClient.AddParameter("pageId",      pageId);
                    dbClient.AddParameter("catalogName", catalogName);
                    dbClient.AddParameter("costCredits", costCredits);
                    dbClient.AddParameter("costPoints",  costPoints);
                    dbClient.AddParameter("pointsType",  pointsType);
                    dbClient.AddParameter("amount",      amount);
                    dbClient.AddParameter("clubOnly",    clubOnly == 1 ? "1" : "0");
                    dbClient.AddParameter("extradata",   extradata ?? "");
                    dbClient.AddParameter("haveOffer",   haveOffer ? "1" : "0");
                    dbClient.AddParameter("offerIdGroup",offerIdGroup);
                    dbClient.AddParameter("limitedStack",limitedStack);
                    dbClient.AddParameter("orderNumber", orderNumber);
                    dbClient.AddParameter("id",          offerId);
                }

                dbClient.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Offer saved"));
        }
    }
}
