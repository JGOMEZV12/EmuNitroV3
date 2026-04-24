using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Timers;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class LawCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_law"; }
        }

        public string Parameters
        {
            get { return "%user% %level%"; }
        }

        public string Description
        {
            get { return "Añade un usuario a la lista deseada para un nivel deseado (1 a 5). (COMANDO MUY SERIO)"; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int WantedLevel = 1;
            int NewWantedLevel = 1;
            bool OnProbation = false;
            Wanted NewWanted;
            #endregion

            #region Conditions
            if (Params.Length != 3)
            {
                Session.SendWhisper("Ingrese un nombre de ciudadano y el nivel deseado que desea asignarle. :buscar usuario nivel", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetHabbo() == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().PassiveMode)
            {
                Session.SendWhisper("¡No puedes asignarle cargos a una persona que está en modo pasivo!", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "law"))
            {
                Session.SendWhisper("Sólo un oficial de policía puede utilizar este comando.", 1);
                return;
            }

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().Jailbroken)
            {
                Session.SendWhisper("Es obvio que esta persona ha jailbroken! No hay necesidad de leyes ellos.", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes contratar a alguien que ya está en la cárcel!", 1);
                return;
            }

            if (TargetClient.GetRoomUser() != null)
            {
                if (TargetClient.GetRoomUser().IsAsleep)
                {
                    Session.SendWhisper("¡No puedes juzgar a alguien que no esté jugando el juego ahora mismo!", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            if (int.TryParse(Params[2], out WantedLevel))
            {
                if (WantedLevel > 5 || WantedLevel == 0)
                {
                    Session.SendWhisper("Por favor ingrese un nivel deseado entre 1 y 5!", 1);
                    return;
                }

                string RoomId = TargetClient.GetHabbo().CurrentRoomId.ToString() != "0" ? TargetClient.GetHabbo().CurrentRoomId.ToString() : "Unknown";

                if (TargetClient.GetRoleplay().OnProbation)
                {
                    OnProbation = true;
                    NewWantedLevel = WantedLevel + 1;

                    if (NewWantedLevel > 5)
                        NewWantedLevel = 5;

                    NewWanted = new Wanted(Convert.ToUInt32(TargetClient.GetHabbo().Id), RoomId, NewWantedLevel);
                }
                else
                    NewWanted = new Wanted(Convert.ToUInt32(TargetClient.GetHabbo().Id), RoomId, WantedLevel);

                if (RoleplayManager.WantedList.ContainsKey(TargetClient.GetHabbo().Id))
                {
                    int CurrentWantedLevel = RoleplayManager.WantedList[TargetClient.GetHabbo().Id].WantedLevel;
                    if ((OnProbation && NewWantedLevel > CurrentWantedLevel) || (!OnProbation && WantedLevel > CurrentWantedLevel))
                    {
                        if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("wanted"))
                            TargetClient.GetRoleplay().TimerManager.ActiveTimers["wanted"].EndTimer();

                        if (!OnProbation)
                        {
                            if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                                TargetClient.GetRoleplay().TimerManager.ActiveTimers["probation"].EndTimer();

                            TargetClient.GetRoleplay().IsWanted = true;
                            TargetClient.GetRoleplay().WantedLevel = WantedLevel;
                            if(WantedLevel == 2)
                                TargetClient.GetRoleplay().WantedTimeLeft = 4;
                            else if (WantedLevel == 3)
                                TargetClient.GetRoleplay().WantedTimeLeft = 6;
                            else if (WantedLevel == 4)
                                TargetClient.GetRoleplay().WantedTimeLeft = 8;
                            else if (WantedLevel == 5)
                                TargetClient.GetRoleplay().WantedTimeLeft = 10;
                            else
                                TargetClient.GetRoleplay().WantedTimeLeft = 2;

                            TargetClient.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                            RoleplayManager.WantedList.TryUpdate(TargetClient.GetHabbo().Id, NewWanted, RoleplayManager.WantedList[TargetClient.GetHabbo().Id]);
                            Session.SendWhisper("¡Actualizaciones de " + TargetClient.GetHabbo().Username + "'s nivel de búsqueda de " + CurrentWantedLevel + " estrella(s) a " + WantedLevel + " estrella(s)!", 34);
                            //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s)!");
                            // WS Wanted Stars
                            if (TargetClient.GetRoleplay().WebSocketConnection != null)
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TargetClient, "compose_wanted_stars|" + TargetClient.GetRoleplay().WantedLevel);

                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                            {
                                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (client == null || client.GetHabbo() == null)
                                        continue;

                                    client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] La policia está buscando a: " + TargetClient.GetHabbo().Username + ", ¡Ayudanos a encontrarlo!", 33);
                                }
                            }
                            return;
                        }
                        else
                        {
                            if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                                TargetClient.GetRoleplay().TimerManager.ActiveTimers["probation"].EndTimer();

                            TargetClient.GetRoleplay().OnProbation = false;
                            TargetClient.GetRoleplay().ProbationTimeLeft = 0;

                            TargetClient.GetRoleplay().IsWanted = true;
                            TargetClient.GetRoleplay().WantedLevel = NewWantedLevel;
                            if (NewWantedLevel == 2)
                                TargetClient.GetRoleplay().WantedTimeLeft = 4;
                            else if (NewWantedLevel == 3)
                                TargetClient.GetRoleplay().WantedTimeLeft = 6;
                            else if (NewWantedLevel == 4)
                                TargetClient.GetRoleplay().WantedTimeLeft = 8;
                            else if (NewWantedLevel == 5)
                                TargetClient.GetRoleplay().WantedTimeLeft = 10;
                            else
                                TargetClient.GetRoleplay().WantedTimeLeft = 2;

                            TargetClient.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                            RoleplayManager.WantedList.TryUpdate(TargetClient.GetHabbo().Id, NewWanted, RoleplayManager.WantedList[TargetClient.GetHabbo().Id]);
                            Session.SendWhisper("¡Actualizaciones de " + TargetClient.GetHabbo().Username + "'s nivel de búsqueda de " + CurrentWantedLevel + " estrella(s) a " + NewWantedLevel + " estrella(s) [+1 debido a la libertad condicional]!", 37);
                            //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + "'s Wanted Level se ha actualizado desde " + CurrentWantedLevel + " Star(s) to " + WantedLevel + " Star(s) [+1 due to Probation]!");
                            // WS Wanted Stars
                            if (TargetClient.GetRoleplay().WebSocketConnection != null)
                                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TargetClient, "compose_wanted_stars|" + TargetClient.GetRoleplay().WantedLevel);

                            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                            {
                                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                                {
                                    if (client == null || client.GetHabbo() == null)
                                        continue;

                                    client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] La policia está buscando a: " + TargetClient.GetHabbo().Username + ", ¡Ayudanos a encontrarlo!", 33);
                                }
                            }
                            return;
                        }
                    }
                    else
                    {
                        Session.Shout("*Trata de actualizar " + TargetClient.GetHabbo().Username + "'s Wanted Level pero notó que su nivel deseado ya está en " + CurrentWantedLevel + " estrella(s)*", 37);
                        return;
                    }
                }
                else
                {
                    if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("wanted"))
                        TargetClient.GetRoleplay().TimerManager.ActiveTimers["wanted"].EndTimer();

                    if (!OnProbation)
                    {
                        TargetClient.GetRoleplay().IsWanted = true;
                        TargetClient.GetRoleplay().WantedLevel = WantedLevel;
                        if (WantedLevel == 2)
                            TargetClient.GetRoleplay().WantedTimeLeft = 4;
                        else if (WantedLevel == 3)
                            TargetClient.GetRoleplay().WantedTimeLeft = 6;
                        else if (WantedLevel == 4)
                            TargetClient.GetRoleplay().WantedTimeLeft = 8;
                        else if (WantedLevel == 5)
                            TargetClient.GetRoleplay().WantedTimeLeft = 10;
                        else
                            TargetClient.GetRoleplay().WantedTimeLeft = 2;
                        TargetClient.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                        RoleplayManager.WantedList.TryAdd(TargetClient.GetHabbo().Id, NewWanted);
                        Session.SendWhisper("¡Se ha agregado a " + TargetClient.GetHabbo().Username + " a la lista de buscados con un nivel de " + WantedLevel + " estrella(s)!", 34);
                        //PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alert] " + TargetClient.GetHabbo().Username + " Ha sido añadido a la Lista de Deseos con un Nivel de " + WantedLevel + " Star(s)!");
                        if (TargetClient.GetRoleplay().WebSocketConnection != null)
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TargetClient, "compose_wanted_stars|" + TargetClient.GetRoleplay().WantedLevel);

                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                        {
                            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null || client.GetHabbo() == null)
                                    continue;

                                client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] La policia está buscando a: " + TargetClient.GetHabbo().Username + ", ¡Ayudanos a encontrarlo!", 33);
                            }
                        }
                        return;
                    }
                    else
                    {
                        if (TargetClient.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("probation"))
                            TargetClient.GetRoleplay().TimerManager.ActiveTimers["probation"].EndTimer();

                        TargetClient.GetRoleplay().OnProbation = false;
                        TargetClient.GetRoleplay().ProbationTimeLeft = 0;

                        TargetClient.GetRoleplay().IsWanted = true;
                        TargetClient.GetRoleplay().WantedLevel = NewWantedLevel;
                        if (NewWantedLevel == 2)
                            TargetClient.GetRoleplay().WantedTimeLeft = 4;
                        else if (NewWantedLevel == 3)
                            TargetClient.GetRoleplay().WantedTimeLeft = 6;
                        else if (NewWantedLevel == 4)
                            TargetClient.GetRoleplay().WantedTimeLeft = 8;
                        else if (NewWantedLevel == 5)
                            TargetClient.GetRoleplay().WantedTimeLeft = 10;
                        else
                            TargetClient.GetRoleplay().WantedTimeLeft = 2;

                        TargetClient.GetRoleplay().TimerManager.CreateTimer("wanted", 1000, false);
                        RoleplayManager.WantedList.TryAdd(TargetClient.GetHabbo().Id, NewWanted);
                        Session.Shout("Se ha agregado a " + TargetClient.GetHabbo().Username + " a la lista de buscados con un Nivel de " + NewWantedLevel + " estrella(s) [+1 libertad condicional]", 37);
                        // PolarEnvironment.GetGame().GetClientManager().JailAlert("[RADIO Alerta] " + TargetClient.GetHabbo().Username + " Ha sido añadido a la Lista de Deseos con un Nivel de " + WantedLevel + " Star(s) [+1 libertad condicional]!");
                        if (TargetClient.GetRoleplay().WebSocketConnection != null)
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TargetClient, "compose_wanted_stars|" + TargetClient.GetRoleplay().WantedLevel);

                        lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
                        {
                            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                            {
                                if (client == null || client.GetHabbo() == null)
                                    continue;

                                client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] La policia está buscando a: " + TargetClient.GetHabbo().Username + ", ¡Ayudanos a encontrarlo!", 33);
                            }
                        }
                        return;
                    }
                }
            }
            else
            {
                Session.SendWhisper("Por favor ingrese un nivel deseado entre 1 y 6", 1);
                return;
            }
            #endregion
        }
    }
}