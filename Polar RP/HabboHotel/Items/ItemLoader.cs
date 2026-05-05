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
                    $"SELECT i.*, " +
                    $"COALESCE(ig.{Polar.Core.DatabaseCompatibility.ItemsGroupIdColumn}, 0) AS group_id, " +
                    $"m.enabled AS mood_enabled, m.current_preset, m.preset_one, m.preset_two, m.preset_three, " +
                    $"t.enabled AS toner_enabled, t.data1, t.data2, t.data3, " +
                    $"w.message AS whisper_message, " +
                    $"h.owner_id AS house_owner, h.cost AS house_cost, h.for_sale AS house_for_sale, h.level AS house_level, " +
                    $"h.is_locked AS house_locked, h.inside_room_id, h.door_x, h.door_y, h.door_z, h.type AS house_type, h.last_forcing " +
                    $"FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` i " +
                    $"LEFT JOIN items_groups ig ON i.id = ig.id " +
                    $"LEFT JOIN room_items_moodlight m ON i.id = m.item_id " +
                    $"LEFT JOIN room_items_toner t ON i.id = t.id " +
                    $"LEFT JOIN room_items_whisper_tile w ON i.id = w.item_id " +
                    $"LEFT JOIN rp_houses h ON i.id = h.sign_id " +
                    $"WHERE i.room_id = @rid");
                dbClient.AddParameter("rid", roomId);
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
                int id = Convert.ToInt32(row["id"]);
                int baseId = Convert.ToInt32(row[baseItemCol]);

                if (!itemManager.GetItem(baseId, out var data))
                    continue;

                items.Add(new Item(
                    id,
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
                    hasWiredData ? Convert.ToString(row["wired_data"]) : "",
                    row));
            }

            return items;
        }

        public static List<Item> GetItemsForUser(int userId)
        {
            DataTable table;
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    $"SELECT i.*, " +
                    $"COALESCE(ig.{Polar.Core.DatabaseCompatibility.ItemsGroupIdColumn}, 0) AS group_id, " +
                    $"m.enabled AS mood_enabled, m.current_preset, m.preset_one, m.preset_two, m.preset_three, " +
                    $"t.enabled AS toner_enabled, t.data1, t.data2, t.data3, " +
                    $"w.message AS whisper_message, " +
                    $"h.owner_id AS house_owner, h.cost AS house_cost, h.for_sale AS house_for_sale, h.level AS house_level, " +
                    $"h.is_locked AS house_locked, h.inside_room_id, h.door_x, h.door_y, h.door_z, h.type AS house_type, h.last_forcing " +
                    $"FROM `{Polar.Core.DatabaseCompatibility.ItemsTable}` i " +
                    $"LEFT JOIN items_groups ig ON i.id = ig.id " +
                    $"LEFT JOIN room_items_moodlight m ON i.id = m.item_id " +
                    $"LEFT JOIN room_items_toner t ON i.id = t.id " +
                    $"LEFT JOIN room_items_whisper_tile w ON i.id = w.item_id " +
                    $"LEFT JOIN rp_houses h ON i.id = h.sign_id " +
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
                    hasWiredData ? Convert.ToString(row["wired_data"]) : "",
                    row));
            }

            return items;
        }
    }
}
