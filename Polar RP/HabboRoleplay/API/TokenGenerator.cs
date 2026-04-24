using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;


namespace Polar.HabboRoleplay.API
{
    public static class TokenGenerator
    {

        private static Random random = new Random();
        

        public static string GenTkn(string Act, int gen_id, int user_id = 0, int app_id = 0, string web_url = null, string extra_data = null)
        {
            RunGen:
            string tkn = RandomString(20);

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `api_token` WHERE token = '" + tkn + "'");
                DataRow ApiTkn = DB.getRow();

                if (ApiTkn != null)
                    goto RunGen;

                DB.RunQuery("INSERT INTO `api_token` (`token`, `action`, `gen_by`, `user_id`, `app_id`, `web_url`, `extra_data`, `time`) VALUES ('"+ tkn +"', '"+ Act +"', "+ gen_id +", "+ user_id +", "+ app_id +", '" + web_url + "', '" + extra_data + "', "+ PolarEnvironment.GetUnixTimestamp() +")");

            }


            return tkn;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
