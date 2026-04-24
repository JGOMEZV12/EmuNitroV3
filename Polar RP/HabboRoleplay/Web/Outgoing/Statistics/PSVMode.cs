using ConnectionManager;
using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Roleplay.Web;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboHotel.Groups;
using Polar.Net;

namespace Polar.HabboRoleplay.Web.Outgoing.Statistics
{
    /// <summary>
    /// TargetWebEvent class.
    /// </summary>
    class PSVWebEvent : IWebEvent
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
                #region Passive Mode
                case "open":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("psvmode", true, true))
                            return;

                        if (Client.GetRoleplay().TogglingPSV)
                        {
                            Client.SendWhisper("Ya te encuentras cambiando el estado de tu modo pasivo.", 1);
                            return;
                        }

                        #region Conditions

                        #region Basic Conditions
                        if (Client.GetRoleplay().Cuffed)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                            return;
                        }
                        if (Client.GetRoomUser() == null || !Client.GetRoomUser().CanWalk)
                        {
                            Client.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsDead)
                        {
                            Client.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsJailed)
                        {
                            Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                            return;
                        }

                        #endregion

                        if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("general"))
                        {
                            Client.SendWhisper("Primero debes de terminar de realizar tu actividad actual.", 1);
                            return;
                        }

                        #region Conditions Checks
                        if (!Client.GetRoomUser().GetRoom().RoomData.SafeZoneEnabled)
                        {
                            Client.SendWhisper("Debes estar en una zona segura para hacer eso.", 1);
                            return;
                        }
                        if (RoleplayManager.PurgeStarted)
                        {
                            Client.SendWhisper("¡No puedes hacer eso durante la purga!", 1);
                            return;
                        }
                        if (!Client.GetRoleplay().PassiveMode)
                        {
                            if (Client.GetRoleplay().CurHealth < Client.GetRoleplay().MaxHealth)
                            {
                                Client.SendWhisper("Debes tener el 100% de vida para hacer eso.", 1);
                                return;
                            }
                        }
                        if (Client.GetRoleplay().IsWanted)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras estás en la lista de buscados.", 1);
                            return;
                        }
                        if (Client.GetRoleplay().IsWorking && GroupManager.HasJobCommand(Client, "law"))
                        {
                            Client.SendWhisper("No puedes hacer eso mientras estás trabajando de policía", 1);
                            return;
                        }
                        if (Client.GetRoleplay().CamCargId == 3 || Client.GetRoleplay().CamCargId == 4)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras transportas cargamentos ilegales.", 1);
                            return;
                        }
                        if (Client.GetRoleplay().EquippedWeapon != null)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras lleves un arma equipada.", 1);
                            return;
                        }
                        #endregion

                        #endregion

                        if (Client.GetRoomUser().IsWalking)
                            Client.GetRoomUser().PathStep = Client.GetRoomUser().Path.Count;

                        string word = (Client.GetRoleplay().PassiveMode) ? "salir del" : "entrar en";

                        Client.GetRoleplay().TogglingPSV = true;
                        Client.GetRoleplay().LoadingTimeLeft = RoleplayManager.PSVTime;

                        RoleplayManager.Shout(Client, "((Comienza a " + word + " modo pasivo))", 7);
                        Client.SendWhisper("Debes esperar " + Client.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                        Client.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("psvmode", 1000, 150);
                        Client.GetRoleplay().SpecialCooldowns.TryUpdate("psvmode", 150, Client.GetRoleplay().SpecialCooldowns["psvmode"]);
                    }
                    break;
                #endregion

                #region Force Desactive Passive Mode
                case "forceoff":
                    {
                        if (Client.GetRoleplay().PassiveMode)
                        {
                            Client.GetRoleplay().PassiveMode = false;
                            Client.SendMessage(new RoomBubbleNotificationComposer("psv-icon", "Modo Pasivo: Desactivado"));
                            Socket.SendWS( "compose_psv_mode|desactive");
                            Client.GetRoomUser().ApplyEffect(EffectsList.None);
                        }
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
