using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminMovePageEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            int    pageId      = packet.PopInt();
            int    newParentId = packet.PopInt();
            int    newIndex    = packet.PopInt();
            string pageTypeStr = packet.PopString();
            bool   isBuilder   = pageTypeStr?.ToUpper() == "BUILDER";
            string tableName   = isBuilder ? "catalog_pages_bc" : "catalog_pages";

            // -1 = toggle enabled, -2 = toggle visible
            if (newParentId == -1)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery(
                        $"UPDATE `{tableName}` SET `enabled` = IF(`enabled` = '1', '0', '1') WHERE `id` = @id");
                    dbClient.AddParameter("id", pageId);
                    dbClient.RunQuery();
                }
                session.SendMessage(new CatalogAdminResultComposer(true, "Page toggled"));
                return;
            }

            if (newParentId == -2)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery(
                        $"UPDATE `{tableName}` SET `visible` = IF(`visible` = '1', '0', '1') WHERE `id` = @id");
                    dbClient.AddParameter("id", pageId);
                    dbClient.RunQuery();
                }
                session.SendMessage(new CatalogAdminResultComposer(true, "Visibility toggled"));
                return;
            }

            // Mover: actualizar parent y orden
            using (IQueryAdapter dbClient2 = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient2.SetQuery(
                    $"UPDATE `{tableName}` SET `parent_id` = @parentId, `order_num` = @orderNum WHERE `id` = @id");
                dbClient2.AddParameter("parentId", newParentId);
                dbClient2.AddParameter("orderNum", newIndex);
                dbClient2.AddParameter("id",       pageId);
                dbClient2.RunQuery();
            }

            session.SendMessage(new CatalogAdminResultComposer(true, "Page moved"));
        }
    }
}
