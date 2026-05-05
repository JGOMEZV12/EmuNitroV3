using System;
using System.Data;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.Database.Interfaces;


namespace Polar.HabboHotel.Items
{
    public static class ItemLoader
    {
        public static List<Item> GetItemsForRoom(int roomId, Room room)
        {
            DataTable table;
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    $"SELECT i.*, COALESCE(ig.{Polar.Core.DatabaseCompatibility.ItemsGroupIdColumn}, 0) AS group_id " +
                    $"FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` i " +
                    $"LEFT JOIN items_groups ig ON i.id = ig.id " +
                    $"WHERE i.room_id = @rid");
                dbClient.AddParameter("rid", roomId);
                table = dbClient.getTable();
            }

            if (table == null || table.Rows.Count == 0)
                return new List<Item>();

            // Verificar columnas opcionales UNA sola vez, no por fila
            bool hasLimitedNumber = table.Columns.Contains("limited_number");
            bool hasLimitedStack = table.Columns.Contains("limited_stack");
            bool hasWiredData = table.Columns.Contains("wired_data");
            string baseItemCol = Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn;
            var itemManager = PolarEnvironment.GetGame().GetItemManager();

            var items = new List<Item>(table.Rows.Count); // capacidad exacta = 0 reallocations

            foreach (DataRow row in table.Rows)
            {
                // Convert.ToInt32 sobre el object nativo es más rápido que ToString()+Parse
                int baseId = Convert.ToInt32(row[baseItemCol]);

                if (!itemManager.GetItem(baseId, out var data))
                    continue;

                items.Add(new Item(
                    Convert.ToInt32(row["id"]),
                    Convert.ToInt32(row["room_id"]),
                    baseId,
                    Convert.ToString(row["extra_data"]),
                    Convert.ToInt32(row["x"]),
                    Convert.ToInt32(row["y"]),
                    Convert.ToDouble(row["z"]),
                    Convert.ToInt32(row["rot"]),
                    Convert.ToInt32(row["user_id"]),
                    Convert.ToInt32(row["group_id"]),
                    hasLimitedNumber ? Convert.ToInt32(row["limited_number"]) : 0,
                    hasLimitedStack ? Convert.ToInt32(row["limited_stack"]) : 0,
                    Convert.ToString(row["wall_pos"]),
                    room,
                    null, null, null,
                    hasWiredData ? Convert.ToString(row["wired_data"]) : ""));
            }

            return items;
        }

        public static List<Item> GetItemsForUser(int userId)
        {
            DataTable table;
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    $"SELECT i.*, COALESCE(ig.{Polar.Core.DatabaseCompatibility.ItemsGroupIdColumn}, 0) AS group_id " +
                    $"FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` i " +
                    $"LEFT OUTER JOIN items_groups ig ON i.id = ig.id " +
                    $"WHERE i.room_id = 0 AND i.user_id = @uid");
                dbClient.AddParameter("uid", userId);
                table = dbClient.getTable();
            }

            if (table == null || table.Rows.Count == 0)
                return new List<Item>();

            bool hasLimitedNumber = table.Columns.Contains("limited_number");
            bool hasLimitedStack = table.Columns.Contains("limited_stack");
            bool hasWiredData = table.Columns.Contains("wired_data");
            string baseItemCol = Polar.Core.DatabaseCompatibility.ItemsBaseItemColumn;
            var itemManager = PolarEnvironment.GetGame().GetItemManager();

            var items = new List<Item>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
            {
                int baseId = Convert.ToInt32(row[baseItemCol]);

                if (!itemManager.GetItem(baseId, out var data))
                    continue;

                items.Add(new Item(
                    Convert.ToInt32(row["id"]),
                    Convert.ToInt32(row["room_id"]),
                    baseId,
                    Convert.ToString(row["extra_data"]),
                    Convert.ToInt32(row["x"]),
                    Convert.ToInt32(row["y"]),
                    Convert.ToDouble(row["z"]),
                    Convert.ToInt32(row["rot"]),
                    Convert.ToInt32(row["user_id"]),
                    Convert.ToInt32(row["group_id"]),
                    hasLimitedNumber ? Convert.ToInt32(row["limited_number"]) : 0,
                    hasLimitedStack ? Convert.ToInt32(row["limited_stack"]) : 0,
                    Convert.ToString(row["wall_pos"]),
                    null, null, null, null,
                    hasWiredData ? Convert.ToString(row["wired_data"]) : ""));
            }

            return items;
        }
    }
}
