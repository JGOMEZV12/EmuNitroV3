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
                // Verificar que existe
                dbClient.SetQuery("SELECT COUNT(*) FROM `items_base` WHERE `id` = @id");
                dbClient.AddParameter("id", id);
                if (dbClient.getInteger() == 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false, $"Item not found: {id}"));
                    return;
                }

                // Verificar instancias colocadas
                dbClient.SetQuery("SELECT COUNT(*) FROM `items` WHERE `base_item` = @id");
                dbClient.AddParameter("id", id);
                int usageCount = dbClient.getInteger();
                if (usageCount > 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false,
                        $"Cannot delete: {usageCount} instances exist in the game"));
                    return;
                }

                // Verificar referencias de catálogo
                dbClient.SetQuery("SELECT COUNT(*) FROM `catalog_items` WHERE `item_ids` LIKE @pattern");
                dbClient.AddParameter("pattern", $"%{id}%");
                int catalogCount = dbClient.getInteger();
                if (catalogCount > 0)
                {
                    session.SendMessage(new FurniEditorResultComposer(false,
                        $"Cannot delete: item is referenced by {catalogCount} catalog entries"));
                    return;
                }

                // Eliminar
                dbClient.SetQuery("DELETE FROM `items_base` WHERE `id` = @id");
                dbClient.AddParameter("id", id);
                dbClient.RunQuery();
            }

            PolarEnvironment.GetGame().GetItemManager().InitAsync();
            session.SendMessage(new FurniEditorResultComposer(true, "Item deleted"));
        }
    }
}