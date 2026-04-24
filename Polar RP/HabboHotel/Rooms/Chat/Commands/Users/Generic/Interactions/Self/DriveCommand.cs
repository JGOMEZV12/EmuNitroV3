using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboRoleplay.Vehicles;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.VehiclesJobs;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Inventory.Bots;
using System.Drawing;
using System.Diagnostics.Eventing.Reader;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class DriveCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_drive"; }
        }

        public string Parameters
        {
            get { return "%tipo%"; }
        }

        public string Description
        {
            get { return "Le permite conducir su avion o su carro."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            try
            {
                //Console.WriteLine($"DriveCommand ejecutado con {Params.Length} parámetros: {string.Join(", ", Params)}");

                // Si no hay parámetros adicionales y ya está conduciendo, detener
                /*if (Params.Length == 1)
                {
                    if (Session.GetRoleplay().DrivingCar == true)
                    {
                        StopCar(Session);
                        return;
                    }
                    else
                    {
                        Session.SendWhisper("Uso: :manejar [avion] o simplemente párate sobre un vehículo y usa :manejar carro", 1);
                        return;
                    }
                }*/

                // Verificar si ya está conduciendo
                if (Session.GetRoleplay().DrivingCar)
                {
                    StopCar(Session);
                    return;
                }

                // Validaciones generales
                if (Session.GetRoleplay().DrivingInCar)
                {
                    Session.SendWhisper("No puedes hacer eso mientras vas de pasajer@.", 1);
                    return;
                }

                if (Session.GetRoleplay().IsDead)
                {
                    Session.SendWhisper("No puedes hacer eso mientras estás muert@.", 1);
                    return;
                }

                if (Session.GetRoleplay().Cuffed)
                {
                    Session.SendWhisper("No puedes hacer eso mientras estás esposad@.", 1);
                    return;
                }

                if (Session.GetRoleplay().TryGetCooldown("car"))
                    return;

                if (Session.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("turfcapture") || Session.GetRoleplay().CapturingTurf != null)
                {
                    Session.SendWhisper("Usted no puede conducir su coche mientras captura un barrio", 1);
                    return;
                }

                if (!Room.DriveEnabled)
                {
                    Session.SendWhisper("¡No puedes conducir tu coche en esta habitación!", 1);
                    return;
                }

                // Lógica para Avión (verificar que Params[1] existe)
                if (Params.Length > 1 && Params[1].ToLower() == "avion")
                {
                    await HandleAirplane(Session, Room);
                    return;
                }

                // Lógica para vehículos terrestres
                await HandleGroundVehicle(Session, Room);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ERROR en DriveCommand: {ex.Message}");
                //Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Session.SendWhisper("Ha ocurrido un error al procesar el comando.", 1);
            }
        }

        private async Task HandleAirplane(GameClient Session, Room Room)
        {
            try
            {
                //Console.WriteLine("Procesando comando para avión");

                if (Session.GetRoleplay().TryGetCooldown("avion"))
                    return;

                if (Session.GetRoleplay().TryGetCooldown("avionew", true))
                    return;

               

                // Si ya está conduciendo algo, detenerlo primero
                if (Session.GetRoleplay().DrivingCar == true)
                    StopCar(Session);

                // Configuración de vuelo
                Session.GetRoleplay().DrivingCar = true;
                Session.GetRoleplay().DrivingInCar = false;
                Session.GetRoleplay().DrivingCarItem = 0;

                // Combustible y parámetros del vehículo
                Session.GetRoleplay().CarType = 3;
                Session.GetRoleplay().CarMaxFuel = 400;
                Session.GetRoleplay().CarFuel = 400;
                Session.GetRoleplay().CarTimer = 0;
                Session.GetRoleplay().CarLife = 100;

                Session.GetRoleplay().CarEnableId = 685;
                Session.GetRoleplay().CarEffectId = 685;

                if (Session.GetRoomUser() != null)
                {
                    Session.GetRoomUser().ApplyEffect(817);
                    Session.GetRoomUser().FastWalking = true;
                }

                RoleplayManager.Shout(Session, "*Encendió su avión*", 5);
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "open");
                Session.GetRoleplay().CooldownManager.CreateCooldown("avion", 1000, 30);
                Session.GetRoleplay().CooldownManager.CreateCooldown("avionew", 1000, 30);

                //Console.WriteLine("Avión iniciado exitosamente");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ERROR en HandleAirplane: {ex.Message}");
                Session.SendWhisper("Error al procesar el avión.", 1);
            }
        }

        private async Task HandleGroundVehicle(GameClient Session, Room Room)
        {
            try
            {
                //Console.WriteLine("Procesando comando para vehículo terrestre");

                #region Get Veihcle Info (play_vehicles)  
                Vehicle vehicle = null;
                bool found = false;
                int itemfurni = 0, corp = 0;
                HabboHotel.Items.Item BTile = null;
                string itemnm = null;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (!found)
                    {
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().Coordinate);
                        if (BTile != null)
                        {
                            vehicle = Vehicle;
                            itemfurni = BTile.Id;
                            itemnm = Vehicle.ItemName;
                            corp = Convert.ToInt32(Vehicle.CarCorp);
                            found = true;
                        }
                    }
                }
                //Al examinar todos los autos ninguno conincide con el item donde está parado el user...
                if (!found)
                {
                    Session.SendWhisper("¡Debes estar sobre un vehículo para conducir!", 1);
                    return;
                }
                #endregion

                #region Corp > 0 then Valid my Job
                Group JobInfo = null;

                if (corp > 0)
                {
                    //Console.WriteLine($"Vehículo de corporación: {corp}");
                    List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

                    if (Groups.Count <= 0)
                    {
                        Session.SendWhisper("No tienes ningún trabajo para conducir este vehículo.", 1);
                        return;
                    }

                    JobInfo = GroupManager.GetJob(corp);

                    int GroupNumber = -1;
                    int count = 0;
                    foreach (var GPO in Groups)
                    {
                        if (corp == GPO.Id)
                            GroupNumber = count;
                        count++;
                    }

                    if (JobInfo != null)
                    {
                        if (GroupNumber <= -1)
                        {
                            Session.SendWhisper("No perteneces al trabajo de " + JobInfo.Name + " para conducir este vehículo.", 1);
                            return;
                        }
                    }

                    Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
                    Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
                }
                #endregion

                List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);

                if (VO == null || VO.Count <= 0)
                {
                    if (corp <= 0)
                    {
                        RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba conducir.", 4);
                        RoleplayManager.PickItem(Session, itemfurni);
                        return;
                    }
                    else
                    {
                        if (VehicleJobsManager.getVehicleJobIDByPos(Room.Id, Session.GetRoomUser().X, Session.GetRoomUser().Y) > 0)
                        {
                            RoleplayManager.VehiclesOwnedID++;
                            VehiclesOwned nVO;

                            if (!PolarEnvironment.GetGame().GetVehiclesOwnedManager().NewVehicleOwned(
                                RoleplayManager.VehiclesOwnedID, itemfurni, vehicle.ItemID, 0,
                                Session.GetHabbo().Id, vehicle.Model, vehicle.MaxFuel, 0, 0,
                                false, false, Room.Id, Session.GetRoomUser().X,
                                Session.GetRoomUser().Y, Session.GetRoomUser().Z,
                                Array.Empty<string>(), false, out nVO))
                            {
                                RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Session.GetHabbo().Username + " intentaba conducir.", 4);
                                RoleplayManager.PickItem(Session, itemfurni);
                                return;
                            }
                            else
                            {
                                VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                                if (VO == null || VO.Count <= 0)
                                {
                                    RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Session.GetHabbo().Username + " intentaba conducir.", 4);
                                    RoleplayManager.PickItem(Session, itemfurni);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Session.GetHabbo().Username + " intentaba conducir.", 4);
                            RoleplayManager.PickItem(Session, itemfurni);
                            return;
                        }
                    }
                }

                #region Grúa Checks
                if (VO[0].Location <= 0)
                {
                    RoleplayManager.Shout(Session, "* Una grúa ha pasado a recoger el vehículo que " + Session.GetHabbo().Username + " intentaba conducir.", 4);
                    RoleplayManager.PickItem(Session, itemfurni);
                    return;
                }
                #endregion

                #region Vehicle Fuel/Traba/State Checks
                if (VO[0].Fuel <= 0)
                {
                    Session.SendWhisper("Este vehículo no tiene combustible.", 1);
                    return;
                }

                if (VO[0].Traba)
                {
                    if (VO[0].State == 1)
                    {
                        Session.SendWhisper("Este vehículo está bloqueado con traba de seguridad. Si es tuyo usa :abrircarro.", 1);
                        return;
                    }
                }

                if (VO[0].State >= 2 || VO[0].CarLife <= 0)
                {
                    Session.SendWhisper("Vehículo averiado. Usa :servicio mecanico para que lo repare.", 1);
                    return;
                }
                #endregion

                #region Vehicle Setup and Driving
                Session.GetRoleplay().DrivingCar = true;
                Session.GetRoleplay().DrivingInCar = false;
                Session.GetRoleplay().DrivingCarId = VO[0].Id;
                Session.GetRoleplay().DrivingCarItem = itemfurni;

                Session.GetRoleplay().CarType = vehicle.CarType;
                Session.GetRoleplay().CarMaxFuel = vehicle.MaxFuel;
                Session.GetRoleplay().CarFuel = VO[0].Fuel;
                Session.GetRoleplay().CarTimer = VO[0].Km;
                Session.GetRoleplay().CarLife = VO[0].CarLife;

                Session.GetRoleplay().CarEnableId = vehicle.EffectID;
                Session.GetRoleplay().CarEffectId = vehicle.EffectID;

                if (Session.GetRoomUser() != null)
                {
                    Session.GetRoomUser().ApplyEffect(vehicle.EffectID);
                    Session.GetRoomUser().FastWalking = true;
                }

                RoleplayManager.PickItem(Session, itemfurni);//Recoge el item
                RoleplayManager.Shout(Session, "* Encendió su coche *", 5);
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "open");
                #endregion

                #region Job Special Checks & Messages
                #region Check My Car & Alarms
                if (Session.GetHabbo().Id == VO[0].OwnerId)
                {
                    Session.SendWhisper("Este vehículo es tuyo.", 1);
                }
                else
                {
                    if (VO[0].Alarm)
                    {
                        string Owner = PolarEnvironment.GetGame().GetClientManager().GetNameById(VO[0].OwnerId);
                        RoomUser Own = Room.GetRoomUserManager().GetRoomUserByHabbo(VO[0].OwnerId);
                        if (Own != null)
                        {
                            Own.GetClient().SendMessage(new RoomNotificationComposer("UK202", "ALARMA: Alguien ha tomado tu " + VO[0].Model + " en " + Room.Name, ""));
                        }
                        RoleplayManager.Shout(Session, "* Se puede escuchar fuertemente la alarma del Vehículo que " + Session.GetHabbo().Username + " conduce.", 4);
                    }
                }
                #endregion

                if (JobInfo != null)
                {
                    #region Basurero
                    if (JobInfo.Name.Contains("Basurero"))
                    {
                        Session.GetRoleplay().IsBasuChofer = true;
                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session,
                            "compose_basurero|" +
                            "showinfo|" +
                            Session.GetHabbo().Username + "|" + // Chofer
                            Session.GetHabbo().Username + "|" + // Recolector
                            Session.GetRoleplay().BasuTrashCount + "/15|" +
                            Session.GetRoleplay().IsBasuChofer);
                    }
                    #endregion

                    #region Camionero
                    if (JobInfo.Name.Contains("Camioneros"))
                    {
                        string ChofName = (VO[0].CamOwnId > 0) ? PolarEnvironment.GetGame().GetClientManager().GetNameById(VO[0].CamOwnId) : "Ninguno.";
                        Session.SendWhisper("Chofer Asignado: " + ChofName + " - Cargamento: " + RoleplayManager.getCamCargName(VO[0].CamCargId) + ".", 1);

                        string DestName = "?";
                        if (RoleplayManager.GenerateRoom(VO[0].CamDest, out Room DestRoom))
                            DestName = DestRoom.Name;

                        if (!ChofName.Equals("Ninguno."))
                        {
                            string action = VO[0].CamState == 2 ? "entregar" : "depositar";
                            string cargaName = VO[0].CamState == 2 ? "Ninguno" : RoleplayManager.getCamCargName(VO[0].CamCargId);
                            PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session,
                                "compose_camionero|showinfo|" + action + "|" + cargaName + "|" + ChofName + "|" + DestName);
                        }
                    }
                    #endregion
                }
                #endregion

                Session.GetRoleplay().CooldownManager.CreateCooldown("car", 1000, 3);
                //Console.WriteLine("Vehículo iniciado exitosamente");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ERROR en HandleGroundVehicle: {ex.Message}");
                //Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Session.SendWhisper("Error al procesar el vehículo.", 1);
            }
        }

        public void StopCar(GameClient Session)
        {
            try
            {
                //Console.WriteLine("Deteniendo vehículo...");

                if (Session.GetRoleplay().CarEnableId == 685) // Avión
                {
                    // Retornamos a valores predeterminados
                    Session.GetRoleplay().DrivingCar = false;
                    Session.GetRoleplay().DrivingInCar = false;

                    // Combustible System
                    Session.GetRoleplay().CarType = 0;
                    Session.GetRoleplay().CarFuel = 0;
                    Session.GetRoleplay().CarMaxFuel = 0;
                    Session.GetRoleplay().CarTimer = 0;
                    Session.GetRoleplay().CarLife = 0;

                    Session.GetRoleplay().CarEnableId = 0;
                    Session.GetRoleplay().CarEffectId = 0;

                    if (Session.GetRoomUser() != null)
                    {
                        Session.GetRoomUser().ApplyEffect(0);
                        Session.GetRoomUser().FastWalking = false;
                    }

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");
                    Session.GetRoleplay().CooldownManager.CreateCooldown("avionew", 1000, 30);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("avion", 1000, 30);
                    //Console.WriteLine("Avión detenido");
                }
                else // Vehículo terrestre
                {
                    #region Get Position User Vars
                    RoomUser User = Session.GetRoomUser();

                    if (User == null)
                        return;

                    int X = User.X;
                    int Y = User.Y;
                    double Z = User.Z;
                    int Rot = User.RotBody;
                    #endregion

                    #region Get Information from VehiclesManager
                    Vehicle vehicle = null;
                    int corp = 0;
                    bool ToDB = true;

                    foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                    {
                        if (Session.GetRoleplay().CarEffectId == Vehicle.EffectID)
                        {
                            vehicle = Vehicle;
                            corp = Convert.ToInt32(Vehicle.CarCorp);
                            if (corp > 0)
                                ToDB = false;
                            break;
                        }
                    }

                    if (vehicle == null)
                    {
                        Session.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                        return;
                    }
                    #endregion

                    // Colocamos Furni en Sala
                    Session.GetRoleplay().isParking = true;
                    HabboHotel.Items.Item Item = RoleplayManager.PutItemToRoom(Session, Session.GetRoleplay().DrivingCarItem,
                        Session.GetRoomUser().RoomId, vehicle.ItemID, X, Y, Rot, ToDB);
                    Session.GetRoleplay().isParking = false;

                    // Actualizamos datos del auto en el diccionario y DB
                    VehiclesOwned VOD;
                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(Session, Item.Id, ToDB, out VOD);

                    #region CorpCar Respawn
                    if (corp > 0)
                    {
                        Session.GetRoleplay().CarJobLastItemId = Item.Id;
                    }
                    #endregion

                    #region Pasajeros
                    string Pasajeros = Session.GetRoleplay().Pasajeros;
                    if (!string.IsNullOrEmpty(Pasajeros))
                    {
                        string[] pasajerosArray = Pasajeros.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string psjs in pasajerosArray)
                        {
                            GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                            if (PJ != null && PJ.GetRoleplay() != null && PJ.GetRoomUser() != null)
                            {
                                if (PJ.GetRoleplay().ChoferName == Session.GetHabbo().Username)
                                {
                                    RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Session.GetHabbo().Username + "*", 5);
                                }

                                // PASAJERO
                                PJ.GetRoleplay().Pasajero = false;
                                PJ.GetRoleplay().ChoferName = "";
                                PJ.GetRoleplay().ChoferID = 0;
                                PJ.GetRoomUser().CanWalk = true;
                                PJ.GetRoomUser().FastWalking = false;
                                PJ.GetRoomUser().TeleportEnabled = false;
                                PJ.GetRoomUser().AllowOverride = false;

                                // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                if (PJ.GetRoleplay().IsBasuPasaj)
                                    PJ.GetRoleplay().IsBasuPasaj = false;

                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");
                            }
                        }

                        // CHOFER 
                        Session.GetRoleplay().PasajerosCount = 0;
                        Session.GetRoleplay().Pasajeros = "";
                        Session.GetRoleplay().Chofer = false;
                        Session.GetRoomUser().AllowOverride = false;
                    }
                    #endregion

                    #region Check Jobs
                    if (Session.GetRoleplay().BasuTeamId <= 0)
                        Session.GetRoleplay().IsBasuChofer = false;
                    #endregion

                    #region Online ParkVars
                    // Retornamos a valores predeterminados
                    Session.GetRoleplay().DrivingCar = false;
                    Session.GetRoleplay().DrivingInCar = false;
                    Session.GetRoleplay().DrivingCarId = 0;

                    // Combustible System
                    Session.GetRoleplay().CarType = 0;
                    Session.GetRoleplay().CarFuel = 0;
                    Session.GetRoleplay().CarMaxFuel = 0;
                    Session.GetRoleplay().CarTimer = 0;
                    Session.GetRoleplay().CarLife = 0;

                    Session.GetRoleplay().CarEnableId = 0;
                    Session.GetRoleplay().CarEffectId = 0;

                    if (Session.GetRoomUser() != null)
                    {
                        Session.GetRoomUser().ApplyEffect(0);
                        Session.GetRoomUser().FastWalking = false;
                    }
                    #endregion

                    RoleplayManager.Shout(Session, "*Detuvo el motor de su vehículo*", 5);
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");
                    Session.GetRoleplay().CooldownManager.CreateCooldown("park", 1000, 3);

                    if (corp > 0 && (VOD.CamOwnId == Session.GetHabbo().Id || VOD.CamOwnId == 0))
                    {
                        int time = RoleplayManager.VehicleJobTime; // 5 mins
                        if (vehicle.Model.Contains("Patrulla"))
                            time = RoleplayManager.VehicleJobPoliTime; // 10 mins.

                        Session.SendWhisper("Recuerda no abandonar mucho tiempo tu vehículo de trabajo o será decomisado.", 1);
                        Session.GetRoleplay().VehicleTimer = time;
                        Session.GetRoleplay().TimerManager.CreateTimer("vehiclejob", 1000, true);
                    }

                    //Console.WriteLine("Vehículo terrestre detenido");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"ERROR en StopCar: {ex.Message}");
                //Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Session.SendWhisper("Error al detener el vehículo.", 1);
            }
        }
    }
}