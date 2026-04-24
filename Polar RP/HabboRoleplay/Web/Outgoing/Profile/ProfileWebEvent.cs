using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using Polar.Database.Interfaces;
using System.Data;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class ProfileWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;
            /*
            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            string ReceivedData = (Data.Contains(',') ? Data.Split(',')[1] : Data);
            //Console.WriteLine(ReceivedData);
            string CachedTargetString;

            GameClient Session = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Convert.ToInt32(ReceivedData));

            if (Session == null || Session.GetHabbo() == null)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT * FROM `users` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", ReceivedData);
                    DataRow dRow = dbClient.getRow();

                    dbClient.SetQuery("SELECT * FROM `rp_stats` WHERE `id` = @id LIMIT 1");
                    dbClient.AddParameter("id", ReceivedData);
                    DataRow dRowRP = dbClient.getRow();

                    if (dRow != null && dRowRP != null)
                    {
                        CachedTargetString = GetUserComponent.ReturnWebUserStatistics(dRow, dRowRP);
                        string SendData = "";
                        SendData += CachedTargetString;
                        Socket.SendWS( "compose_userprofile|" + SendData);
                    }

                    dRow = null;
                    dRowRP = null;
                }
            }
            else
            {
                CachedTargetString = GetUserComponent.ReturnWebUserStatistics(Session);
                string SendData = "";
                SendData += CachedTargetString;
                Socket.SendWS( "compose_userprofile|" + SendData);
                
            }

        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
