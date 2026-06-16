using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorDeleteEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new FurniEditorResultComposer(false, "No permission"));
                return;
            }

            int id = packet.PopInt();
            if (id <= 0)
            {
                session.SendMessage(new FurniEditorResultComposer(false, "Invalid item ID"));
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery($"SELECT COUNT(*) FROM `{DatabaseCompatibility.FurnitureTable}` WHERE `id` = @id");
                dbClient.AddParameter("id", id);
                if (dbClient.getInteger() == 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false, $"Item not found: {id}"));
                    return;
                }

                dbClient.SetQuery($"SELECT COUNT(*) FROM `items` WHERE `{DatabaseCompatibility.ItemsBaseItemColumn}` = @id");
                dbClient.AddParameter("id", id);
                int usageCount = dbClient.getInteger();
                if (usageCount > 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false,
                        $"Cannot delete: {usageCount} instances exist in the game"));
                    return;
                }

                dbClient.SetQuery("SELECT COUNT(*) FROM `catalog_items` WHERE `item_ids` LIKE @pattern OR `item_ids` = @idstr");
                dbClient.AddParameter("pattern", $"%{id}%");
                dbClient.AddParameter("idstr", id.ToString());
                int catalogCount = dbClient.getInteger();
                if (catalogCount > 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false,
                        $"Cannot delete: item is referenced by {catalogCount} catalog entries"));
                    return;
                }

                dbClient.SetQuery($"DELETE FROM `{DatabaseCompatibility.FurnitureTable}` WHERE `id` = @id");
                dbClient.AddParameter("id", id);
                dbClient.RunQuery();
            }

            PolarEnvironment.GetGame().GetItemManager().InitAsync();
            session.SendMessage(new FurniEditorResultComposer(true, "Item deleted"));
        }
    }
}