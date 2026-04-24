using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboHotel.Roleplay.Web;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// TargetWebEvent class.
    /// </summary>
    class TargetWebEvent : IWebEvent
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
                #region Combat Mode
                case "open":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("combatmode", true))
                            return;

                        Client.GetRoleplay().CombatMode = !Client.GetRoleplay().CombatMode;

                        if (Client.GetRoleplay().CombatMode)
                        {
                            Client.SendMessage(new RoomBubbleNotificationComposer("combat-icon", "Modo Combate: Activado"));
                            Socket.SendWS( "compose_combat_mode|active");
                        }
                        else
                        {
                            Client.SendMessage(new RoomBubbleNotificationComposer("combat-icon", "Modo Combate: Desactivado"));
                            Socket.SendWS( "compose_combat_mode|desactive");
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("combatmode", 1000, 3);
                    }
                    break;
                #endregion


                #region lock
                case "lock":
                    {
                        Client.GetRoleplay().TargetLock = true;
                    }
                    break;
                #endregion

                #region unlock
                case "unlock":
                    {
                        Client.GetRoleplay().TargetLock = false;
                    }
                    break;
                #endregion

                #region close
                case "close":
                    {
                        Client.GetRoleplay().TargetLock = false;
                        Client.GetRoleplay().Target = "";
                    }
                    break;
                #endregion

                #region Default
                default:
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
