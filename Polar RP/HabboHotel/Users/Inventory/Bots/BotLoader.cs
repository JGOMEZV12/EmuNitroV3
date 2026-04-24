using System;
using System.Data;
using System.Collections.Generic;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Users.Inventory.Bots
{
    internal static class BotLoader
    {
        public static List<Bot> GetBotsForUser(int userId)
        {
            var bots = new List<Bot>();

            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT `id`,`user_id`,`name`,`motto`,`look`,`gender` " +
                "FROM `bots` " +
                "WHERE `user_id` = @uid AND `room_id` = '0' AND `ai_type` != 'pet'");
            dbClient.AddParameter("uid", userId);

            DataTable table = dbClient.getTable();
            if (table == null) return bots;

            foreach (DataRow row in table.Rows)
            {
                bots.Add(new Bot(
                    Convert.ToInt32(row["id"]),
                    Convert.ToInt32(row["user_id"]),
                    Convert.ToString(row["name"]),
                    Convert.ToString(row["motto"]),
                    Convert.ToString(row["look"]),
                    Convert.ToString(row["gender"])));
            }

            return bots;
        }
    }
}
