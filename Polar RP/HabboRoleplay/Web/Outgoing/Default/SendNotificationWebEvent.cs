using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fleck;

using Polar.HabboHotel.GameClients;
using System.IO;
using ConnectionManager;
using Polar.Net;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Default
{
    /// <summary>
    /// SendNotificationWebEvent class.
    /// </summary>
    class SendNotificationWebEvent : IWebEvent
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

            string[] parameters = Data.Split(',');

            if (parameters.Length < 1)
                return;

            Socket.SendWS("compose_jsalert|" + parameters[0]);
        }
    }
}
