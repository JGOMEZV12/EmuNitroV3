using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using Polar.Net;
using Newtonsoft.Json;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Roleplay.Web.Incoming.Others
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class WantedWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {


            Dictionary<object, object> ReturnedData = JsonConvert.DeserializeObject<Dictionary<object, object>>(Data);
            string Action = null;

            if (ReturnedData.ContainsKey("action"))
                Action = Convert.ToString(ReturnedData["action"]);


            switch (Action.ToLower())
            {

                #region Open
                case "open":
                    {
                        StringBuilder Message = new StringBuilder();
                        int WantedStar = 0;
                        if (RoleplayManager.WantedList.Count <= 0)
                            Socket.SendWS( "compose_buscado|none|[]");

                        Message.Append("[");
                        lock (RoleplayManager.WantedList.Values)
                        {
                            foreach (var Wanted in RoleplayManager.WantedList.Values)
                            {

                                if (Wanted.WantedLevel == 1) WantedStar = 1;
                                if (Wanted.WantedLevel == 2) WantedStar = 2;
                                if (Wanted.WantedLevel == 3) WantedStar = 3;
                                if (Wanted.WantedLevel == 4) WantedStar = 4;
                                if (Wanted.WantedLevel == 5) WantedStar = 5;
                                if (Wanted.WantedLevel == 6) WantedStar = 6;

                                WantedUserList wantedlist = new WantedUserList
                                {
                                    username = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Wanted.UserId)).Username,
                                    look = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Wanted.UserId)).Look,
                                    stars = WantedStar,
                                    last_seen = Wanted.LastSeenRoom,
                                };
                                Message.Append(JsonConvert.SerializeObject(wantedlist, Formatting.Indented) + ",");
                             }
                        }
                        Message.Append("]");
                        Message.Replace(",]", "]");

                        //System.IO.File.WriteAllText(@"C:\patients.json", Message.ToString());
                        Socket.SendWS( "compose_buscado|" + Message);
                    }
                    break;
               #endregion
            }
        }
    }
    public class WantedUserList
    {

        public string username { get; set; }
        public string look { get; set; }
        public int stars { get; set; }
        public string last_seen { get; set; }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}