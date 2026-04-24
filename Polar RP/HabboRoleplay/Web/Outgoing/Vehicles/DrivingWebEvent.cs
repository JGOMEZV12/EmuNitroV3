using ConnectionManager;
using Polar.Net;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboRoleplay.Vehicles;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.VehiclesJobs;
using System.Drawing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// VehiclesWebEvent class.
    /// </summary>
    class DrivingWebEvent : IWebEvent
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
            /*
            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
            Room Room = Client.GetHabbo().CurrentRoom;

             switch (Action)
            {

                #region Driving
                case "driving":
                    {

                        if (!Client.GetRoleplay().DrivingCar)
                        {
                            if (Client.GetRoleplay().DrivingInCar)
                            {
                                Client.SendWhisper("No puedes hacer eso mientras vas de pasajer@.", 1);
                                return;
                            }
                            if (Client.GetRoleplay().IsDead)
                            {
                                Client.SendWhisper("No puedes hacer eso mientras estás muert@.", 1);
                                return;
                            }
                            if (Client.GetRoleplay().Cuffed)
                            {
                                Client.SendWhisper("No puedes hacer eso mientras estás esposad@.", 1);
                                return;
                            }

                            if (Client.GetRoleplay().TryGetCooldown("car"))
                                return;

                            if (Client.GetRoleplay().TimerManager.ActiveTimers.ContainsKey("turfcapture") || Client.GetRoleplay().CapturingTurf != null)
                            {
                                Client.SendWhisper("Usted no puede conducir su coche mientras captura un barrio", 1);
                                return;
                            }

                            if (!Room.DriveEnabled)
                            {
                                Client.SendWhisper("¡No puedes conducir tu coche en esta habitación!", 1);
                                return;
                            }

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
                                    BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Client.GetRoomUser().Coordinate);
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
                                Client.SendWhisper("¡Debes estar sobre un vehículo para conducir!", 1);
                                return;
                            }
                            #endregion

                            Group JobInfo = null;
                            #region Corp > 0 then Valid my Job
                            if (corp > 0)
                            {

                                // Set GetRoleplay().JobId
                                #region Group Conditions
                                List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                                if (Groups.Count <= 0)
                                {
                                    Client.SendWhisper("No tienes ningún trabajo para conducir este vehículo.", 1);
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
                                        Client.SendWhisper("No perteneces al trabajo de " + JobInfo.Name + " para conducir este vehículo.", 1);
                                        return;
                                    }
                                }

                                Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                                Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                                #endregion

                                #region Job Conditions
                                PolarEnvironment.GetGame().GetGroupManager().TryGetGroup(corp, out JobInfo);
                                if (JobInfo != null)
                                {
                                    if (Client.GetRoleplay().JobId != corp)
                                    {
                                        Client.SendWhisper("No perteneces al trabajo de " + JobInfo.Name + " para conducir este vehículo.", 1);
                                        return;
                                    }
                                    if (vehicle.DisplayName == "Camión VIP")
                                    {
                                        if (Client.GetHabbo().VIPRank != 1)
                                        {
                                            Client.SendWhisper("¡Solo VIP pueden conducir estos camiones!", 1);
                                            return;
                                        }
                                    }
                                    if (JobInfo.Ranks.ToList().Where(x => x.Value.MaleFigure.Length > 0).Count() > 0)
                                    {
                                        if (!Client.GetRoleplay().IsWorking)
                                        {
                                            Client.SendWhisper("¡Debes tener el uniforme para conducir este vehiculo!", 1);
                                            return;
                                        }
                                    }
                                }
                                #endregion
                            }
                            #endregion

                            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                            if (VO == null || VO.Count <= 0)
                            {
                                if (corp <= 0)
                                {
                                    RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo que " + Client.GetHabbo().Username + " intentaba conducir.", 4);
                                    RoleplayManager.PickItem(Client, itemfurni);
                                    return;
                                }
                                else
                                {
                                    // Verificamos que esté en posición de autos corp
                                    if (VehicleJobsManager.getVehicleJobIDByPos(Room.Id, Client.GetRoomUser().X, Client.GetRoomUser().Y) > 0)
                                    {
                                        // Insertamos en diccionario. Si no se puede insertar...
                                        RoleplayManager.VehiclesOwnedID++;
                                        VehiclesOwned nVO;
                                        if (!PolarEnvironment.GetGame().GetVehiclesOwnedManager().NewVehicleOwned(RoleplayManager.VehiclesOwnedID, itemfurni, vehicle.ItemID, 0, Client.GetHabbo().Id, vehicle.Model, vehicle.MaxFuel, 0, 0, false, false, Room.Id, Client.GetRoomUser().X, Client.GetRoomUser().Y, Client.GetRoomUser().Z, string.Empty.ToString().Split(';'), false, out nVO))
                                        {
                                            RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Client.GetHabbo().Username + " intentaba conducir.", 4);
                                            RoleplayManager.PickItem(Client, itemfurni);
                                            return;
                                        }
                                        else
                                        {
                                            // Volvemos a pedir el diccionario
                                            VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                                            if (VO == null || VO.Count <= 0)
                                            {
                                                RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Client.GetHabbo().Username + " intentaba conducir.", 4);
                                                RoleplayManager.PickItem(Client, itemfurni);
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo de trabajo que " + Client.GetHabbo().Username + " intentaba conducir.", 4);
                                        RoleplayManager.PickItem(Client, itemfurni);
                                        return;
                                    }
                                }
                            }

                            #region Extra Conditions & Checks
                            #region Grúa Checks (Lotacion <= 0)
                            if (VO[0].Location <= 0)
                            {
                                RoleplayManager.Shout(Client, "* Una grúa ha pasado a recoger el vehículo que " + Client.GetHabbo().Username + " intentaba conducir.", 4);
                                RoleplayManager.PickItem(Client, itemfurni);
                                return;
                            }
                            #endregion

                            #region Vehicle Fuel/Traba/State Checks
                            //Anexar Condiciones para autos de empresas.
                            if (VO[0].Fuel <= 0)
                            {
                                Client.SendWhisper("Este vehículo no tiene combustible.", 1);
                                return;
                            }

                            if (VO[0].Traba)
                            {
                                if (VO[0].State == 1)
                                {
                                    Client.SendWhisper("Este vehículo está bloqueado con traba de seguridad. Si es tuyo usa :abrircarro.", 1);
                                    return;
                                }
                            }
                            //state = 0 -> Normal
                            //state = 1 -> Bloqueado con traba
                            //state = 2 -> No traba y Averiado
                            //state = 3 -> Con traba y Averiado
                            //state = 4 -> Grua
                            if (VO[0].State >= 2 || VO[0].CarLife <= 0)
                            {
                                Client.SendWhisper("Vehículo averiado. Usa :servicio mecanico para que lo repare.", 1);
                                return;
                            }
                            #endregion

                            #region CorpCar Respawn
                            //if (corp > 0)
                            //{
                            // Si conduce un auto de trabajo, pero ya había conducido uno NEW => Cualquier auto que conduzca
                            // Verificamos si es el mismo.
                            // Si es otro, recogemos el anterior (de existir) y spawn.
                            if (Client.GetRoleplay().CarJobId > 0 && Client.GetRoleplay().CarJobLastItemId != itemfurni)
                            {
                                /*VERFICAR SI LO TRAE CONDUCIENDO ALGUIEN PARA EVITAR DUPLICAR*/
                                // Quitar del diccionario
                                /*PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwnedByFurniId(Session.GetRoleplay().CarJobLastItemId);
                                //Recoge el item
                                RoleplayManager.PickItem(Session, Session.GetRoleplay().CarJobLastItemId);
                                //Respawneamos nuevo auto
                                RoleplayManager.SetJobCar(Session, Session.GetRoleplay().CarJobId);*/
                                RoleplayManager.CheckCorpCarp(Client);
                                Client.GetRoleplay().CamCargId = 0;
                                Client.SendWhisper("Tu vehículo de trabajo anterior ha sido recogido por conducir uno diferente.", 1);
                            }

                            if (Client.GetRoleplay().CarJobId <= 0)
                                Client.GetRoleplay().CarJobId = VehicleJobsManager.getVehicleJobID(Room.Id, corp, Client.GetRoomUser().X, Client.GetRoomUser().Y);
                            if (corp > 0)
                                Client.GetRoleplay().CarJobLastItemId = itemfurni;
                            else
                                Client.GetRoleplay().CarJobLastItemId = 0;
                            //}
                            #endregion
                            #endregion

                            #region Online DriveVars
                            //Lo conduce
                            Client.GetRoleplay().DrivingCar = true;
                            Client.GetRoleplay().DrivingInCar = false;
                            Client.GetRoleplay().DrivingCarId = VO[0].Id;// Id de VehiclesOwned;
                            Client.GetRoleplay().DrivingCarItem = itemfurni;

                            //Combustible System
                            Client.GetRoleplay().CarType = vehicle.CarType;// Define el gasto de combustible
                            Client.GetRoleplay().CarMaxFuel = vehicle.MaxFuel;
                            Client.GetRoleplay().CarFuel = VO[0].Fuel;
                            Client.GetRoleplay().CarTimer = VO[0].Km;
                            Client.GetRoleplay().CarLife = VO[0].CarLife;

                            Client.GetRoleplay().CarEnableId = vehicle.EffectID;//Coloca el enable para conducir
                            Client.GetRoleplay().CarEffectId = vehicle.EffectID;//Guarda el enable del último auto en conducción.
                            Client.GetRoomUser().ApplyEffect(vehicle.EffectID);
                            Client.GetRoomUser().FastWalking = true;

                            if (vehicle.FastCar > 0)
                            {
                                Client.GetRoleplay().FastCarNew = vehicle.FastCar;
                            }
                            #endregion

                            if (Client.GetRoomUser() != null)
                            {
                                if (Client.GetRoomUser().CurrentEffect != Client.GetRoleplay().CarEnableId)
                                    Client.GetRoomUser().ApplyEffect(Client.GetRoleplay().CarEnableId);
                            }

                            RoleplayManager.PickItem(Client, itemfurni);//Recoge el item
                            RoleplayManager.Shout(Client, "*Encendió el motor de su vehículo*", 5);
                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "open");// WS FUEL

                            #region Job Special Checks & Messages
                            #region Check My Car & Alarms
                            if (Client.GetHabbo().Id == VO[0].OwnerId)
                            {
                                Client.SendWhisper("Este vehículo es tuyo.", 1);
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
                                    RoleplayManager.Shout(Client, "* Se puede escuchar fuertemente la alarma del Vehículo que " + Client.GetHabbo().Username + " conduce.", 4);
                                }
                            }
                            #endregion

                            if (JobInfo != null)
                            {
                                #region Basurero
                                if (JobInfo.Name.Contains("Basurero"))
                                {
                                    string recolectorName = "Ninguno";
                                    Client.GetRoleplay().IsBasuChofer = true;

                                    /* if (Client.GetRoleplay().BasuTeamId <= 0)
                                     {
                                         Client.SendWhisper("Recuerda que necesitas un compañero que recolecte la basura. ¡Píde a otro trabajador de Basurero que aborde a tu Camión!", 1);
                                         Client.GetRoleplay().IsBasuChofer = true;
                                     }
                                     else*/
                                    recolectorName = Client.GetRoleplay().BasuTeamName;

                                    PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client,
                                            "compose_basurero|" +
                                            "showinfo|" +
                                            Client.GetHabbo().Username + "|" + // Chofer
                                            Client.GetHabbo().Username + "|" + // Recolector
                                            Client.GetRoleplay().BasuTrashCount + "/15|" +
                                            Client.GetRoleplay().IsBasuChofer);
                                }
                                #endregion

                                #region Camionero
                                if (JobInfo.Name.Contains("Camioneros"))
                                {
                                    string ChofName = (VO[0].CamOwnId > 0) ? PolarEnvironment.GetGame().GetClientManager().GetNameById(VO[0].CamOwnId) : "Ninguno.";
                                    Client.SendWhisper("Chofer Asignado: " + ChofName + " - Cargamento: " + RoleplayManager.getCamCargName(VO[0].CamCargId) + ".", 1);

                                    string DestName = "?";
                                    if (RoleplayManager.GenerateRoom(VO[0].CamDest, out Room DestRoom))
                                        DestName = DestRoom.Name;

                                    if (!ChofName.Equals("Ninguno."))
                                    {
                                        string action = VO[0].CamState == 2 ? "entregar" : "depositar";
                                        string cargaName = VO[0].CamState == 2 ? "Ninguno" : RoleplayManager.getCamCargName(VO[0].CamCargId);
                                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_camionero|showinfo|" + action + "|" + cargaName + "|" + ChofName + "|" + DestName);
                                    }
                                }
                                #endregion

                                #endregion
                                
                                Client.GetRoleplay().CooldownManager.CreateCooldown("car", 1000, 3);
                                return;
                            }
                            Socket.SendWS( "compose_carnew|stop|");
                        }
                        else
                        {

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_driving", "stopdriving");// WS FUEL
                            Socket.SendWS( "compose_carnew|close");
                            Socket.SendWS( "compose_carnew|open");
                        }
                    }
                    break;
                #endregion

                #region Stop Driving
                case "stopdriving":
                    {
                        #region Get Position User Vars
                        RoomUser User = Client.GetRoomUser();

                        if (User == null)
                            return;

                        int X = User.X;
                        int Y = User.Y;
                        double Z = User.Z;
                        int Rot = User.RotBody;
                        Point Coords = User.Coordinate;
                        #endregion

                        #region Get Information form VehiclesManager
                        Vehicle vehicle = null;
                        int corp = 0;
                        bool ToDB = true;
                        foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                        {
                            if (Client.GetRoleplay().CarEffectId == Vehicle.EffectID)
                            {
                                vehicle = Vehicle;
                                corp = Convert.ToInt32(Vehicle.CarCorp);
                                if (corp > 0)
                                    ToDB = false;
                            }
                        }
                        if (vehicle == null)
                        {
                            Client.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                            return;
                        }
                        #endregion

                        // Colocamos Furni en Sala
                        Client.GetRoleplay().isParking = true;
                        HabboHotel.Items.Item Item = RoleplayManager.PutItemToRoom(Client, Client.GetRoleplay().DrivingCarItem, Client.GetRoomUser().RoomId, vehicle.ItemID, X, Y, Rot, ToDB);
                        //Item Item = RoleplayManager.PlaceItemToRoom(Client, vehicle.ItemID, 0, X, Y, Z, Rot, false, Room.Id, ToDB, "");
                        Client.GetRoleplay().isParking = false;

                        // Actualizamos datos del auto en el diccionario y DB
                        VehiclesOwned VOD;
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(Client, Item.Id, ToDB, out VOD);

                        #region CorpCar Respawn
                        if (corp > 0)
                        {
                            Client.GetRoleplay().CarJobLastItemId = Item.Id;
                        }
                        #endregion

                        #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                        //Vars
                        string Pasajeros = Client.GetRoleplay().Pasajeros;
                        string[] stringSeparators = new string[] { ";" };
                        string[] result;
                        result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string psjs in result)
                        {
                            GameClient PJ = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                            if (PJ != null && PJ.GetRoleplay() != null && PJ.GetRoomUser() != null)
                            {
                                if (PJ.GetRoleplay().ChoferName == Client.GetHabbo().Username)
                                {
                                    RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
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

                                PJ.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(PJ.GetRoomUser()));
                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                            }
                        }

                        // CHOFER 
                        Client.GetRoleplay().PasajerosCount = 0;
                        Client.GetRoleplay().Pasajeros = "";
                        Client.GetRoleplay().Chofer = false;
                        Client.GetRoomUser().AllowOverride = false;
                        #endregion

                        #region Check Jobs

                        #region Basurero
                        if (Client.GetRoleplay().BasuTeamId <= 0)
                            Client.GetRoleplay().IsBasuChofer = false;
                        #endregion

                        #endregion

                        #region Online ParkVars
                        //Retornamos a valores predeterminados
                        Client.GetRoleplay().DrivingCar = false;
                        Client.GetRoleplay().DrivingInCar = false;
                        Client.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                        //Combustible System
                        Client.GetRoleplay().CarType = 0;// Define el gasto de combustible
                        Client.GetRoleplay().CarFuel = 0;
                        Client.GetRoleplay().CarMaxFuel = 0;
                        Client.GetRoleplay().CarTimer = 0;
                        Client.GetRoleplay().CarLife = 0;

                        Client.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                        Client.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                        Client.GetRoomUser().ApplyEffect(0);
                        Client.GetRoomUser().FastWalking = false;
                        #endregion

                        if (vehicle.FastCar > 0)
                        {
                            Client.GetRoleplay().FastCarNew = 0;
                        }
                        RoleplayManager.Shout(Client, "*Detuvo el motor de su vehículo*", 5);
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "close");// WS FUEL
                        Client.GetRoleplay().CooldownManager.CreateCooldown("park", 1000, 3);

                        if (corp > 0 && (VOD.CamOwnId == Client.GetHabbo().Id || VOD.CamOwnId == 0))
                        {
                            int time = RoleplayManager.VehicleJobTime; // 5 mins
                            if (vehicle.Model.Contains("Patrulla"))
                                time = RoleplayManager.VehicleJobPoliTime; // 10 mins.

                            Client.SendWhisper("Recuerda no abandonar mucho tiempo tu vehículo de trabajo o será decomisado.", 1);
                            Client.GetRoleplay().VehicleTimer = time;
                            Client.GetRoleplay().TimerManager.CreateTimer("vehiclejob", 1000, true);
                        }
                        return;

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
