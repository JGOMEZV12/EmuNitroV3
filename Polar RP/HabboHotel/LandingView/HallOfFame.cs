using Polar.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.LandingView
{
   public class HallOfFame
    {

            public List<hallOfFameWinner> StaffList;
            public HallOfFame()
            {
                StaffList = new List<hallOfFameWinner>();
                StaffList.Clear();

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `users` INNER JOIN `rp_stats` ON `users`.`id` = `rp_stats`.`id` WHERE `rp_stats`.`level` >= 1 ORDER BY `rp_stats`.`level` DESC LIMIT 16");
                    DataTable staff = dbClient.getTable();

                    if (staff != null)
                    {
                        foreach (DataRow Data in staff.Rows)
                        {
                            StaffList.Add(new hallOfFameWinner(Data["username"].ToString(), Convert.ToInt32(Data["level"]), Data["look"].ToString(), Convert.ToInt32(Data["id"])));
                        }
                    }
                }
                return;
            }

        

        public class hallOfFameWinner
        {
            public string username;
            public int level;
            public string look;
            public int id;

            public hallOfFameWinner(string username, int level, string look, int id)
            {
                this.username = username;
                this.level = level;
                this.look = look;
                this.id = id;
            }
        }
    }
}
