using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;

namespace Polar.HabboHotel.Roleplay.Web.OutGoing.Misc
{
    /// <summary>
    /// CaptchaWebEvent class.
    /// </summary>
    class CaptchaWebEvent : IWebEvent
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

            string Action = Data.Split(',')[0];
            string Title = Data.Split(',')[1];

            switch (Action)
            {
                case "create":
                    Socket.SendWS( "compose_captchabox|" + Data);
                    Client.GetRoleplay().CaptchaSent = true;
                    break;

                case "complete":
                    Client.GetRoleplay().CaptchaSent = false;
                    Client.GetRoleplay().CaptchaTime = 0;
                    break;

                case "regenerate":
                    Client.GetRoleplay().CreateCaptcha(Title);
                    break;
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
