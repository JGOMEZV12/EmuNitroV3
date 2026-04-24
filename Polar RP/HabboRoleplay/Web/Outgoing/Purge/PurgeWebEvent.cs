using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Net;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// PhoneWebEvent class.
    /// </summary>
    class PurgeWebEvent : IWebEvent
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
            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);


            switch (Action)
            {
                #region Open
                case "open":
                    {
                        Socket.SendWS( "compose_purge|open|");
                    }
                    break;
                #endregion


                #region Close
                case "close":
                    {
                        Socket.SendWS( "compose_purge|close|");
                    }
                    break;
                #endregion

                #region Timer
                case "timer":
                    {
                        string[] ReceivedData = Data.Split(',');
                        Socket.SendWS( "compose_purge|timer|" + ReceivedData[1]);
                    }
                    break;
                #endregion

                #region Timer Off
                case "timer_off":
                    {
                        Socket.SendWS( "compose_purge|timer_off|");
                    }
                    break;
                #endregion
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
