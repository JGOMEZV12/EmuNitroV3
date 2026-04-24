using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Web.Outgoing.Statistics;

namespace Polar.HabboHotel.Roleplay.Web.Incoming.General
{
    /// <summary>
    /// RetrieveStatsWebEvent class.
    /// </summary>
    class RetrieveStatsWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (Client == null)
                return;

            string CachedDataString = GetUserComponent.ReturnUserStatistics(Client);

            if (String.IsNullOrEmpty(CachedDataString))
                return;

            Socket.SendWS( "compose_characterbar|" + CachedDataString);
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}