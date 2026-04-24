using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminCreateOfferEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

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

            bool isBuilder = pageTypeStr?.ToUpper() == "BUILDER";
            string cleanItemIds = string.IsNullOrWhiteSpace(itemIds) ? "0" : itemIds.Trim();

            int newId = -1;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                if (isBuilder)
                {
                    dbClient.SetQuery(
                        "INSERT INTO `catalog_items_bc` (`page_id`,`item_ids`,`catalog_name`,`order_number`,`extradata`) " +
                        "VALUES (@pageId,@itemIds,@catalogName,@orderNumber,@extradata)");
                }
                else
                {
                    dbClient.SetQuery(
                        "INSERT INTO `catalog_items` (`page_id`,`item_ids`,`catalog_name`,`cost_credits`,`cost_points`," +
                        "`points_type`,`amount`,`club_only`,`extradata`,`have_offer`,`offer_id`,`limited_stack`,`order_number`) " +
                        "VALUES (@pageId,@itemIds,@catalogName,@costCredits,@costPoints,@pointsType,@amount," +
                        "@clubOnly,@extradata,@haveOffer,@offerIdGroup,@limitedStack,@orderNumber)");
                }

                dbClient.AddParameter("pageId",      pageId);
                dbClient.AddParameter("itemIds",     cleanItemIds);
                dbClient.AddParameter("catalogName", catalogName);
                dbClient.AddParameter("orderNumber", orderNumber);
                dbClient.AddParameter("extradata",   extradata ?? "");

                if (!isBuilder)
                {
                    dbClient.AddParameter("costCredits",  costCredits);
                    dbClient.AddParameter("costPoints",   costPoints);
                    dbClient.AddParameter("pointsType",   pointsType);
                    dbClient.AddParameter("amount",       amount);
                    dbClient.AddParameter("clubOnly",     clubOnly == 1 ? "1" : "0");
                    dbClient.AddParameter("haveOffer",    haveOffer ? "1" : "0");
                    dbClient.AddParameter("offerIdGroup", offerIdGroup);
                    dbClient.AddParameter("limitedStack", limitedStack);
                }

                newId = (int)dbClient.InsertQuery();
            }

            if (newId > 0)
                session.SendMessage(new CatalogAdminResultComposer(true, $"Offer created: {newId}"));
            else
                session.SendMessage(new CatalogAdminResultComposer(false, "Failed to create offer"));
        }
    }
}
