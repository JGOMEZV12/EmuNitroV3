using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Net;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Purse
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class PurseWebEvent : IWebEvent
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
            DateTime future = DateTime.Now;

            switch (Action)
            {
                #region Credits
                case "credits":
                    {
                        Socket.SendWS( "compose_update_purse|credits|" + (Client.GetHabbo().Credits < 0 ? 0 : Client.GetHabbo().Credits));
                        break;
                    }
                #endregion

                #region Duckets
                case "duckets":
                    {
                        Socket.SendWS( "compose_update_purse|duckets|" + (Client.GetHabbo().Duckets < 0 ? 0 : Client.GetHabbo().Duckets));
                        break;
                    }
                #endregion

                #region Diamonds
                case "diamonds":
                    {
                        Socket.SendWS( "compose_update_purse|diamonds|" + (Client.GetHabbo().Diamonds < 0 ? 0 : Client.GetHabbo().Diamonds));
                        break;
                    }
                #endregion

                #region HC
                case "hc":
                    {
                        Double Expire = Client.GetHabbo().GetClubManager().GetSubscription("habbo_vip").ExpireTime;
                        Double TimeLeft = Expire - PolarEnvironment.GetUnixTimestamp();
                        int TotalDaysLeft = (int)Math.Ceiling(TimeLeft / 86400);
                        future = DateTime.Now.AddDays(TotalDaysLeft);
                        Socket.SendWS( "compose_update_purse|hc|" + TotalDaysLeft);
                        break;
                    }
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
