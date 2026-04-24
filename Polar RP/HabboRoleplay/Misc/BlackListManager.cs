using System;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Weapons;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Timers;

namespace Polar.HabboRoleplay.Misc
{
    public class BlackListManager
    {
        /// <summary>
        /// Thread-safe list containing blacklisted users
        /// </summary>
        public static List<int> BlackList = new List<int>();

        /// <summary>
        /// Gets the blacklisted users from the database
        /// </summary>
        public static void Initialize()
        {
            BlackList.Clear();
            DataTable Table;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * from `rp_blacklist`");
                Table = dbClient.getTable();

                if (Table != null)
                {
                    foreach (DataRow Row in Table.Rows)
                        BlackList.Add(Convert.ToInt32(Row["id"]));
                }
            }
        }

        /// <summary>
        /// Adds the chosen client to the blacklist
        /// </summary>
        /// <param name="Client"></param>
        public static void AddBlackList(int Id)
        {
            if (BlackList.Contains(Id))
                return;

            BlackList.Add(Id);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("INSERT INTO `rp_blacklist` (`id`) VALUES ('" + Id + "')");
            }
        }

        /// <summary>
        /// Removes the chosen client from the blacklist
        /// </summary>
        /// <param name="Client"></param>
        public static void RemoveBlackList(int Id)
        {
            if (!BlackList.Contains(Id))
                return;

            BlackList.Remove(Id);

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `rp_blacklist` WHERE `id` = '" + Id + "'");
            }
        }
    }
}