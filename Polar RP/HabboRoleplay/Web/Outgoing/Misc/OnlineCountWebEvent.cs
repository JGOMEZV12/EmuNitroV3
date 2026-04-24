using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;

namespace Polar.HabboHotel.Roleplay.Web.OutGoing.Misc
{
    /// <summary>
    /// OnlineCountWebEvent class.
    /// </summary>
    class OnlineCountWebEvent : IWebEvent
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

            Socket.SendWS( "compose_newonlinecount|" + Data);
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
