using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Roleplay.Web;


namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// RetrieveUStatsWebEvent class.
    /// </summary>
    class RetrieveUStatsWebEvent : IWebEvent
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

            if (Client.GetRoleplay().TargetLock)
                return;


            string UserID = Data;
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(UserID);

            if (TargetClient == null)
                return;

            string CachedTargetString = GetUserComponent.ReturnUserStatistics(TargetClient);
            if (String.IsNullOrEmpty(CachedTargetString))
                return;

            Socket.SendWS( "compose_characterbar|" + CachedTargetString);
            #region Hospital
            Socket.SendWS( "compose_hospital|close_actionbtn|");
            if (GroupManager.HasJobCommand(Client, "reviewhosp"))
            {
                if (Client.GetRoleplay().IsWorking)
                {
                    // In Hospital
                    if (TargetClient.GetRoleplay().IsDead && !TargetClient.GetRoleplay().BeingHealed)
                    {
                        // Revisar
                        if (Client.GetRoleplay().RevisPaci != TargetClient.GetHabbo().Id)
                        {
                            Socket.SendWS( "compose_hospital|open_actionbtn|Revisar|revisar");
                        }
                        // Atender
                        else
                        {
                            Socket.SendWS( "compose_hospital|open_actionbtn|Atender|atender");
                        }
                    }
                }
            }
            #endregion

        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
