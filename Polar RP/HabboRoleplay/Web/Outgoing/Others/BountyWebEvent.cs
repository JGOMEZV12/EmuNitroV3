using ConnectionManager;
using System;
using System.Data;
using System.Linq;
using System.Text;
using Polar.Net;
using Newtonsoft.Json;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Cache;
using Polar.Database.Interfaces;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Roleplay.Web.Incoming.Others
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class BountyWebEvent : IWebEvent
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
                        //string json = "";
                        if (BountyManager.BountyUsers.Count <= 0)
                            Socket.SendWS( "compose_bounty:[]");

                        Message.Append("[");
                        lock (BountyManager.BountyUsers.Values)
                        {
                            foreach (Bounty Bounty in BountyManager.BountyUsers.Values)
                            {
                                if (PolarEnvironment.GetUnixTimestamp() > Bounty.ExpiryTimeStamp)
                                {
                                    BountyManager.RemoveBounty(Bounty.UserId, true);
                                    Habbo BountyOwner = PolarEnvironment.GetHabboById(Convert.ToInt32(Bounty.AddedBy));

                                    if (BountyOwner == null || BountyOwner.GetClient() == null)
                                        continue;

                                    BountyOwner.GetClient().SendWhisper("La recompensa que usted fijó encendido " + PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Bounty.UserId)).Username + " ha expirado", 1);
                                }

                                TimeSpan Difference = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Bounty.ExpiryTimeStamp).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(PolarEnvironment.GetUnixTimestamp()));
                                BountyUserList usersbounty = new BountyUserList
                                {
                                    username = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Bounty.UserId)).Username,
                                    look = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Bounty.UserId)).Look,
                                    addedBy = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Convert.ToInt32(Bounty.AddedBy)).Username,
                                    reward = Convert.ToInt32(String.Format("{0:N0}", Bounty.Reward)),
                                };
                                Message.Append(JsonConvert.SerializeObject(usersbounty, Formatting.Indented) + ",");
                                
                            }
                        }
                        Message.Append("]");
                        Message.Replace(",]", "]");

                        //Client.SendMessage(new MOTDNotificationComposer(Message.ToString()));
                        //System.IO.File.WriteAllText(@"C:\patients.json", Message.ToString());
                       // Logging.WriteLine("" + Message + "");*/
                        Socket.SendWS( "compose_bounty|" + Message);
                    }
                    break;
               #endregion
            }
        }
    }
    public class BountyUserList
    {

        public string username { get; set; }
        public string look { get; set; }
        public string addedBy { get; set; }
        public int reward { get; set; }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
