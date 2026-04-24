using System;
using System.Collections.Generic;
using System.Linq;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Items;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Threading;

namespace Polar.HabboRoleplay.Timers.Types
{
    /// <summary>
    /// Countdown to nuke the RP
    /// </summary>
    public class NukeTimer : SystemRoleplayTimer
    {
        public NukeTimer(string Type, int Time, bool Forever, object[] Params) 
            : base(Type, Time, Forever, Params)
        {
            // 2 minutes converted to milliseconds
            int NukeTime = RoleplayManager.NukeMinutes;
            TimeLeft = NukeTime * 60000;
            TimeCount = 0;
        }

        /// <summary>
        /// Nuke process
        /// </summary>
        public override void Execute()
        {
            try
            {
                GameClient Nuker = (GameClient)Params[0];

                if (Nuker == null || Nuker.LoggingOut || Nuker.GetHabbo() == null || Nuker.GetRoleplay() == null || Nuker.GetRoleplay().IsDead || Nuker.GetRoleplay().IsJailed)
                {
                    lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                    {
                        foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            if (Client == null || Client.GetHabbo() == null)
                                continue;

                            Client.SendWhisper("[EXTRA NOTICIA] La brigada SWAT han protegido a la ciudad de la actividad sospechosa nuking y la ciudad está marcada como segura! ¡Agradeceles!", 34);
                        }
                    }

                    base.EndTimer();
                    return;
                }

                if (!RoleplayManager.GenerateRoom(Nuker.GetHabbo().CurrentRoomId, out Room Room))
                    return;

                List<Item> Items = Room.GetGameMap().GetRoomItemForSquare(Nuker.GetRoomUser().Coordinate.X, Nuker.GetRoomUser().Coordinate.Y);

                if (Items.Count < 1)
                {
                    RoleplayManager.Shout(Nuker, "*Detiene ataque nuclear*", 4);
                    base.EndTimer();
                    return;
                }

                bool HasCaptureTile = Items.ToList().Where(x => x.GetBaseItem().ItemName == "actionpoint01").ToList().Count() > 0;

                if (!HasCaptureTile)
                {
                    RoleplayManager.Shout(Nuker, "*Detiene el proceso de ataque nuclear*", 4);
                    base.EndTimer();
                    return;
                }

                TimeCount++;
                TimeLeft -= 1000;

                if (TimeLeft > 0)
                {
                    if (TimeCount == 60)
                    {
                        RoleplayManager.Shout(Nuker, "*Se acerca el fin de " + PolarEnvironment.GetConfig().data["hotel.name"] + " [" + (TimeLeft / 60000) + " Minutos restantes]*", 4);

                        #region Warn all on-duty NPA associates

                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                                    continue;

                                if (!GroupManager.HasJobCommand(client, "npa") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                                    continue;

                                if (!client.GetRoleplay().IsWorking)
                                    continue;

                                if (client.GetRoleplay().DisableRadio)
                                    continue;

                                client.SendWhisper("[ATAQUE TERRORISTA] [NUCLEAR] ¡Advertencia! ¡Alguien ha penetrado en la máquina nuclear y lo ha mandado para destruir la ciudad!Tienes " + (TimeLeft / 60000) + " minuto(s) restante para detenerlos rápidamente!", 30);
                            }
                        }

                        #endregion

                        TimeCount = 0;
                    }
                    return;
                }

                int Counter = 15;
                int KillsGained = 0;

                new Thread(() =>
                {
                    while (Counter > 0)
                    {
                        #region Global Warming

                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                        {
                            foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (Client == null || Client.GetHabbo() == null)
                                    continue;

                                Client.SendWhisper("[ATAQUE TERRORISTA] [NUCLEAR] Una bomba nuclear está a punto de salir, debes apurarse al Hospital rápidamente... (" + Counter + " seconds)", 1);
                            }
                        }

                        #endregion

                        Counter--;
                        Thread.Sleep(1000);

                        if (Counter == 0)
                        {
                            #region Kill any un-safe citizens.

                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                                        continue;

                                    if (Client.GetHabbo().GetPermissions().HasRight("mod_tool") && Client.GetRoleplay().StaffOnDuty || Client.GetHabbo().VIPRank > 0)
                                        continue;

                                    if (Client.GetRoleplay().IsJailed || Client.GetRoleplay().IsDead)
                                        continue;

                                    if (Client.GetHabbo().CurrentRoomId == Convert.ToInt32(RoleplayData.GetData("hospital", "roomid")) && Client.GetHabbo().CurrentRoomId == Convert.ToInt32(RoleplayData.GetData("hospital", "roomid2")))
                                        Client.SendWhisper("[GOBIERNO] El arma nuclear ha explotado, pero te han encontrado refugio en el Hospital. ¡Buen trabajo!", 1);
                                    else if (Client.GetHabbo().CurrentRoom.SafeZoneEnabled)
                                        Client.SendWhisper("[GOBIERNO] El arma nuclear ha explotado, pero te han encontrado refugio en una sala de la zona de seguridad. ¡Buen trabajo!", 1);
                                    else if (Client.GetHabbo().CurrentRoomId == Convert.ToInt32(RoleplayData.GetData("npa", "insideroomid")))
                                        Client.SendWhisper("[Alerta segura] El arma nuclear ha explotado, pero te han encontrado refugio en la sala de la NPA. ¡Buen trabajo!", 1);
                                    else
                                    {
                                        // Kill the un-safe users.
                                        KillsGained++;
                                        Client.GetRoleplay().CurHealth = 0;

                                        Client.SendNotification("La bomba nuclear ha explotado, ¡la radioactividad te ha matado");
                                    }
                                }
                            }

                            #region Breaking News

                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                foreach (GameClient ClientAll in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (ClientAll == null || ClientAll.GetHabbo() == null)
                                        continue;

                                    ClientAll.SendWhisper("[ÚLTIMAS NOTICIAS] [ATAQUE] Los asociados de NPA informaron que " + KillsGained + " ¡los ciudadanos murieron en la bomba nuclear!", 33);
                                }
                            }

                            #endregion

                            #endregion
                        }
                    }

                }).Start();

                base.EndTimer();
                return;
            }
            catch (Exception e)
            {
                Logging.LogRPTimersError("Error in Execute() void: " + e);
                base.EndTimer();
            }
        }
    }
}