using System;
using System.Data;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.Core;
using Newtonsoft.Json;

namespace Polar.HabboHotel.Items.Wired
{
    public static class WiredMigration
    {
        public static void Run()
        {
            if (!DatabaseCompatibility.ColumnExists("items", "wired_data"))
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("ALTER TABLE `items` ADD COLUMN `wired_data` TEXT NULL");
                }
                MigrateOldData();
            }
        }

        private static void MigrateOldData()
        {
            DataTable table;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SHOW TABLES LIKE 'wired_items'");
                if (!dbClient.findsResult()) return;

                dbClient.SetQuery("SELECT * FROM `wired_items`");
                table = dbClient.getTable();

                if (table == null || table.Rows.Count == 0) return;

                foreach (DataRow row in table.Rows)
                {
                    int id = Convert.ToInt32(row["id"]);
                    string items = Convert.ToString(row["items"]);
                    int delay = Convert.ToInt32(row["delay"]);
                    string str = Convert.ToString(row["string"]);
                    bool b = Convert.ToInt32(row["bool"]) == 1;

                    var data = new Dictionary<string, object>
                    {
                        { "delay", delay },
                        { "string", str },
                        { "bool", b },
                        { "items", items }
                    };

                    string json = JsonConvert.SerializeObject(data);

                    dbClient.SetQuery("UPDATE `items` SET `wired_data` = @json WHERE `id` = @id");
                    dbClient.AddParameter("json", json);
                    dbClient.AddParameter("id", id);
                    dbClient.RunQuery();
                }
            }
        }
    }
}