using System;
using System.Data;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboHotel.Users.Inventory.Pets
{
    internal static class PetLoader
    {
        public static List<Pet> GetPetsForUser(int userId)
        {
            var pets = new List<Pet>();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT `id`,`user_id`,`room_id`,`name`,`x`,`y`,`z` " +
                    "FROM `bots` " +
                    "WHERE `user_id` = @uid AND `room_id` = '0' AND `ai_type` = 'pet'");
                dbClient.AddParameter("uid", userId);

                DataTable petRows = dbClient.getTable();
                if (petRows == null) return pets;

                foreach (DataRow row in petRows.Rows)
                {
                    int petId = Convert.ToInt32(row["id"]);

                    dbClient.SetQuery(
                        "SELECT `type`,`race`,`color`,`experience`,`energy`,`nutrition`,`respect`," +
                        "`createstamp`,`have_saddle`,`anyone_ride`,`hairdye`,`pethair`,`gnome_clothing` " +
                        "FROM `bots_petdata` WHERE `id` = @petId LIMIT 1");
                    dbClient.AddParameter("petId", petId);

                    DataRow mRow = dbClient.getRow();
                    if (mRow == null) continue;

                    pets.Add(new Pet(
                        petId,
                        Convert.ToInt32(row["user_id"]),
                        Convert.ToInt32(row["room_id"]),
                        Convert.ToString(row["name"]),
                        Convert.ToInt32(mRow["type"]),
                        Convert.ToString(mRow["race"]),
                        Convert.ToString(mRow["color"]),
                        Convert.ToInt32(mRow["experience"]),
                        Convert.ToInt32(mRow["energy"]),
                        Convert.ToInt32(mRow["nutrition"]),
                        Convert.ToInt32(mRow["respect"]),
                        Convert.ToDouble(mRow["createstamp"]),
                        Convert.ToInt32(row["x"]),
                        Convert.ToInt32(row["y"]),
                        Convert.ToDouble(row["z"]),
                        Convert.ToInt32(mRow["have_saddle"]),
                        Convert.ToInt32(mRow["anyone_ride"]),
                        Convert.ToInt32(mRow["hairdye"]),
                        Convert.ToInt32(mRow["pethair"]),
                        Convert.ToString(mRow["gnome_clothing"])));
                }
            }

            // Append roleplay pets owned by this user
            foreach (RoleplayBot rpBot in RoleplayBotManager.CachedRoleplayBots.Values)
            {
                if (rpBot?.IsPet == true && rpBot.OwnerId == userId)
                    pets.Add(rpBot.PetInstance);
            }

            return pets;
        }
    }
}
