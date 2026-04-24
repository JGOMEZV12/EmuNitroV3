using System;
using System.Collections.Generic;
using System.Data;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.FurniEditor;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public class FurniEditorDetailEvent : IPacketEvent
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

            SendDetailResponse(session, id);
        }

        public static void SendDetailResponse(GameClient session, int itemId)
        {
            Dictionary<string, object> item = null;
            int usageCount = 0;
            var catalogItems = new List<Dictionary<string, object>>();
            string furniDataJson = "{}";

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // Cargar item completo
                dbClient.SetQuery("SELECT * FROM `items_base` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", itemId);
                DataRow row = dbClient.getRow();

                if (row == null)
                {
                    session.SendMessage(new FurniEditorResultComposer(false, $"Item not found: {itemId}"));
                    return;
                }

                item = FurniEditorHelper.ReadFullItem(row);

                // Cantidad de instancias colocadas
                dbClient.SetQuery("SELECT COUNT(*) FROM `items` WHERE `base_item` = @id");
                dbClient.AddParameter("id", itemId);
                usageCount = dbClient.getInteger();

                // Referencias de catálogo
                dbClient.SetQuery(
                    "SELECT ci.id AS ci_id, ci.catalog_name, ci.cost_credits, ci.cost_points, ci.points_type, " +
                    "ci.page_id AS ci_page_id, COALESCE(cp.caption, '') AS page_caption " +
                    "FROM `catalog_items` ci " +
                    "LEFT JOIN `catalog_pages` cp ON ci.page_id = cp.id " +
                    "WHERE ci.item_ids LIKE @pattern");
                dbClient.AddParameter("pattern", $"%{itemId}%");

                DataTable catalogTable = dbClient.getTable();
                if (catalogTable != null)
                {
                    foreach (DataRow catalogRow in catalogTable.Rows)
                        catalogItems.Add(FurniEditorHelper.ReadCatalogRef(catalogRow));
                }
            }

            try { furniDataJson = FurniDataManager.GetItemJson(itemId); }
            catch { furniDataJson = "{}"; }

            var packet = new FurniEditorDetailComposer(item, usageCount, catalogItems, furniDataJson);
            byte[] data = packet.GetBytes();
            Console.WriteLine($"Item: {item}");
            Console.WriteLine($"Packet length: {data.Length}");
            Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));
            session.SendMessage(packet);
            //session.SendMessage(new FurniEditorDetailComposer(item, usageCount, catalogItems, furniDataJson));
        }
    }
}