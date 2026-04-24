using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Timers.Types;
using Polar.HabboRoleplay.Timers;
using Polar.HabboHotel.Users.Effects;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.RPRoom;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class PurgeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_purge"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Inicia/Detiene el evento de Purga"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetRoleplay().TryGetCooldown("purge"))
                return;

            if (RoleplayManager.PurgeStarted)
            {
                // Detener purga
                #region Execute
                RoleplayManager.PurgeStarted = false;
                PolarEnvironment.GetGame().GetClientManager().StaffAlertMsg(Session.GetHabbo().Username + " ha detenido el evento de 'La Purga'");

                #region WS
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                        continue;
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_purge", "close");
                }
                #endregion
                #endregion
            }
            else
            {
                // Iniciar purga
                #region Execute

                #region Check Workers & Special Checkers
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                        continue;

                    #region Force Desactivate PSV Mode
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_psv", "forceoff");
                    #endregion

                    if (client.GetRoleplay().IsWorking)
                    {
                        WorkManager.RemoveWorkerFromList(client);
                        client.GetRoleplay().IsWorking = false;
                        client.GetHabbo().Poof();
                        //RoleplayManager.CheckCorpCarp(client);
                        RoleplayManager.Shout(client, "*Ha dejado de trabajar*", 5);

                        #region Check Police Car
                        if (client.GetRoleplay().DrivingCar && client.GetRoleplay().CarEnableId == EffectsList.CarPolice)
                        {
                            #region Park
                            int ItemPlaceId = 0;
                            int roomid = client.GetRoomUser().RoomId;
                           
                            if (!RoleplayManager.GenerateRoom(roomid, out Room Roomx, false))
                                continue;
                            VehiclesOwned VOD = null;
                            if (client.GetRoleplay().DrivingInCar)
                            {
                                RoleplayManager.Shout(client, "* Una Grúa se ha llevado el vehículo que " + client.GetHabbo().Username + " conducía.", 4);
                                // Actualizamos datos del auto en el diccionario y DB
                                PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(client, 0, false, out VOD);
                                ItemPlaceId = VOD.Id;
                                PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(client.GetRoleplay().DrivingCarId);
                                RoleplayManager.CheckCorpCarp(client);
                            }

                            #region Extra Conditions & Checks
                            #region CorpCar Respawn
                            client.GetRoleplay().CarJobLastItemId = ItemPlaceId;
                            #endregion

                            #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                            //Vars
                            string Pasajeros = client.GetRoleplay().Pasajeros;
                            string[] stringSeparators = new string[] { ";" };
                            string[] result;
                            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string psjs in result)
                            {
                                GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                if (PJ != null)
                                {
                                    if (PJ.GetRoleplay().ChoferName == client.GetHabbo().Username)
                                    {
                                        RoleplayManager.Shout(PJ, "*Baja del vehículo de " + client.GetHabbo().Username + "*", 5);
                                    }
                                    // PASAJERO
                                    PJ.GetRoleplay().Pasajero = false;
                                    PJ.GetRoleplay().ChoferName = "";
                                    PJ.GetRoleplay().ChoferID = 0;
                                    PJ.GetRoomUser().CanWalk = true;
                                    PJ.GetRoomUser().FastWalking = false;
                                    PJ.GetRoomUser().TeleportEnabled = false;
                                    PJ.GetRoomUser().AllowOverride = false;

                                    // Descontamos Pasajero
                                    client.GetRoleplay().PasajerosCount--;
                                    StringBuilder builder = new StringBuilder(client.GetRoleplay().Pasajeros);
                                    builder.Replace(PJ.GetHabbo().Username + ";", "");
                                    client.GetRoleplay().Pasajeros = builder.ToString();

                                    // CHOFER 
                                    client.GetRoleplay().Chofer = (client.GetRoleplay().PasajerosCount <= 0) ? false : true;
                                    client.GetRoomUser().AllowOverride = (client.GetRoleplay().PasajerosCount <= 0) ? false : true;

                                    // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                    if (PJ.GetRoleplay().IsBasuPasaj)
                                        PJ.GetRoleplay().IsBasuPasaj = false;
                                }
                            }
                            #endregion

                            #endregion

                            #region Online ParkVars
                            //Retornamos a valores predeterminados
                            client.GetRoleplay().DrivingCar = false;
                            client.GetRoleplay().DrivingInCar = false;
                            client.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                            //Combustible System
                            client.GetRoleplay().CarType = 0;// Define el gasto de combustible
                            client.GetRoleplay().CarFuel = 0;
                            client.GetRoleplay().CarMaxFuel = 0;
                            client.GetRoleplay().CarTimer = 0;
                            client.GetRoleplay().CarLife = 0;

                            client.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                            client.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                            client.GetRoomUser().ApplyEffect(0);
                            client.GetRoomUser().FastWalking = false;
                            #endregion

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_vehicle", "close");
                            #endregion

                            RoleplayManager.CheckCorpCarp(client);
                        }
                        #endregion
                    }
                }
                #endregion

                RoleplayManager.PurgeStarted = true;
                RoleplayManager.TimerManager.CreateTimer("purge", 1000, true);
                PolarEnvironment.GetGame().GetClientManager().StaffAlertMsg(Session.GetHabbo().Username + " ha iniciado el evento de 'La Purga'");

                #region WS
                foreach (GameClient client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null || client.GetRoleplay() == null)
                        continue;
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(client, "event_purge", "open");
                }
                #endregion

                #endregion
            }

            Session.GetRoleplay().CooldownManager.CreateCooldown("purge", 1000, 80);
        }
    }
}
