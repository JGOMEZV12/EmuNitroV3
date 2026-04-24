using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.Net;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using System.IO;
using Polar.HabboRoleplay.Misc;
using System.Collections.Generic;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// CamioneroWebEvent class.
    /// </summary>
    class CamioneroWebEvent : IWebEvent
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
                #region Open
                case "open":
                    {
                        #region Pagas
                        int Amn = 0, Med = 0, Crack = 0, Piezas = 0;

                        if (Client.GetRoleplay().CamLvl == 1)
                        {
                            Amn = 13;
                            Med = 2;
                            Crack = 1;
                            Piezas = 2;
                        }
                        else if (Client.GetRoleplay().CamLvl == 2)
                        {
                            Amn = 16;
                            Med = 4;
                            Crack = 2;
                            Piezas = 5;
                        }
                        else if (Client.GetRoleplay().CamLvl == 3)
                        {
                            Amn = 20;
                            Med = 6;
                            Crack = 3;
                            Piezas = 7;
                        }
                        else if (Client.GetRoleplay().CamLvl == 4)
                        {
                            Amn = 22;
                            Med = 8;
                            Crack = 4;
                            Piezas = 7;
                        }
                        else if (Client.GetRoleplay().CamLvl == 5)
                        {
                            Amn = 25;
                            Med = 10;
                            Crack = 5;
                            Piezas = 7;
                        }
                        else if (Client.GetRoleplay().CamLvl >= 6) // Max Lvl
                        {
                            Amn = 30;
                            Med = 12;
                            Crack = 6;
                            Piezas = 7;
                        }
                        #endregion

                        Client.GetRoleplay().ViewCamCargas = true;
                        Socket.SendWS( "compose_camionero|open|" + Amn + "|" + Med + "|" + Crack + "|" + Piezas + "|");
                    }
                    break;
                #endregion

                #region Cargar
                case "cargar":
                    {
                        if (Client.GetRoleplay().TryGetCooldown("cargcam"))
                            return;

                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;

                        #region Conditions
                        if (RoleplayManager.PurgeStarted)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|¡No puedes trabajar durante la purga!");
                            return;
                        }
                        #endregion

                        #region Group Conditions
                        List<Groups.Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|No tienes ningún trabajo para hacer eso.");
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Socket.SendWS( "compose_camionero|cammsg|((No perteneces a ningún trabajo usar ese comando))");
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Socket.SendWS( "compose_camionero|cammsg|((No perteneces a ningún trabajo para usar ese comando))");
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 0; // Desempleado
                            Client.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Socket.SendWS( "compose_camionero|cammsg|Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.");
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "camionero"))
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Debes tener el trabajo de Camionero para usar ese comando.");
                            return;
                        }

                        if (!Client.GetRoleplay().DrivingCar)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Debes conducir un Camión para hacer eso.");
                            return;
                        }

                        #region Get Information form VehiclesManager
                        Vehicle vehicle = null;
                        int corp = 0;
                        foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                        {
                            if (Client.GetRoleplay().CarEffectId == Vehicle.EffectID)
                            {
                                vehicle = Vehicle;
                                corp = Convert.ToInt32(Vehicle.CarCorp);
                            }
                        }
                        if (vehicle == null)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|¡Ha ocurrido un error al buscar los datos del vehículo que conduces!");
                            return;
                        }
                        #endregion

                        if (!GroupManager.GetJob(corp).Name.Contains("Camioneros"))
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Debes conducir un Camión para hacer eso.");
                            return;
                        }

                        /*string MyCity1 = Room.City;
                        int CamRoomID = PolarEnvironment.GetGame().GetRoleplayRoomManager().TryToGetCamioneros(MyCity1, out PlayRoom mData);//camioneros de la cd.
                        if (Client.GetHabbo().CurrentRoomId != CamRoomID)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|¡Debes estar en la zona de cargamento para Camioneros!");
                            return;
                        }*/
                        #endregion

                        #region Camionero Conditions
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Client.GetRoleplay().DrivingCarId);
                        if (VO == null || VO.Count <= 0)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|((No se pudo obtener información del vehículo que conduces))");
                            return;
                        }
                        if (VO[0].CamOwnId > 0 && VO[0].CamOwnId != Client.GetHabbo().Id)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Este camión ya se encuentra cargado por otra persona.");
                            return;
                        }
                        // Para controlar que cargue solo un camion a la vez.
                        if (Client.GetRoleplay().CamCargId > 0)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Ya has cargado un Camión. No puedes hacer más de un recorrido a la vez. Usa ':abandonarcarga' para comenzar uno nuevo.");
                            return;
                        }
                        if (VO[0].CamState > 0)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Tu camión ya fue cargado. ¡Ve a entregar la carga a tu destino!");
                            return;
                        }

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Debes estar en la zona de Cargamento para Cargar tu camión.");
                            return;
                        }
                        #endregion

                        if (Client.GetRoleplay().IsCamLoading)
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Ya te encuentras cargando el camión. Por favor espera...");
                            return;
                        }
                        #endregion

                        #region Execute
                        int ID;
                        string[] ReceivedData = Data.Split(',');
                        if (int.TryParse(ReceivedData[1], out ID))
                        {
                            if (ID < 1 || ID > 4)
                            {
                                Socket.SendWS( "compose_camionero|cammsg|ID de carga inválida. Usa :cargas para ver un listado de ellas.");
                                return;
                            }

                           /* if (Client.GetRoleplay().PassiveMode && (ID == 3 || ID == 4))
                            {
                                Socket.SendWS( "compose_camionero|cammsg|¡No puedes llevar cargamentos ilegales en modo pasivo!");
                                return;
                            }*/

                            if (RoleplayManager.getCamCargDest(Room, ID) < 1)
                            {
                                Client.SendNotification("Al parecer no hay destinos para entregar " + RoleplayManager.getCamCargName(ID) + " en esta Ciudad. ((Contacta con un Administrador))");
                                return;
                            }


                            VO[0].CamDest = RoleplayManager.getCamCargDest(Room, ID);
                            Client.GetRoleplay().CamCargId = ID;

                            // Timer
                            Client.GetRoleplay().IsCamLoading = true;
                            Client.GetRoleplay().LoadingTimeLeft = RoleplayManager.CamCargTime;

                            RoleplayManager.Shout(Client, "*Comienza a cargar su camión*", 5);
                            Client.SendWhisper("Debes esperar " + Client.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                            Client.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
                            Client.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);

                            Client.GetRoleplay().ViewCamCargas = false;
                            Socket.SendWS( "compose_camionero|close|");
                        }
                        else
                        {
                            Socket.SendWS( "compose_camionero|cammsg|Ingresa una ID válida. ((:cargarcamion [ID]))");
                            return;
                        }
                        #endregion
                    }
                    break;
                #endregion

                #region Depositar
                case "depositar":
                    {
                        #region Conditions

                        if (Client.GetRoleplay().TryGetCooldown("cargcam"))
                            return;

                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;
                        #endregion

                        #region Group Conditions
                        List<Groups.Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Client.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Client.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Client.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 0; // Desempleado
                            Client.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Client.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "camionero"))
                        {
                            Client.SendWhisper("Debes tener el trabajo de Camionero para usar ese comando.", 1);
                            return;
                        }
                        if (!Client.GetRoleplay().DrivingCar)
                        {
                            Client.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                            return;
                        }

                        #region Get Information form VehiclesManager
                        Vehicle vehicle = null;
                        int corp = 0;
                        foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                        {
                            if (Client.GetRoleplay().CarEffectId == Vehicle.EffectID)
                            {
                                vehicle = Vehicle;
                                corp = Convert.ToInt32(Vehicle.CarCorp);
                            }
                        }
                        if (vehicle == null)
                        {
                            Client.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                            return;
                        }
                        #endregion

                        if (!Client.GetRoleplay().DrivingCar || !GroupManager.GetJob(corp).Name.Contains("Camioneros"))
                        {
                            Client.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                            return;
                        }

                        #endregion

                        #region Camionero Conditions
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Client.GetRoleplay().DrivingCarId);
                        if (VO == null || VO.Count <= 0)
                        {
                            Client.SendWhisper("((No se pudo obtener información del vehículo que conduces))", 1);
                            return;
                        }
                        if (VO[0].CamOwnId > 0 && VO[0].CamOwnId != Client.GetHabbo().Id)
                        {
                            Client.SendWhisper("Este camión no ha sido cargado bajo tu nombre. No puedes hacer recorridos ajenos.", 1);
                            return;
                        }
                        if (VO[0].CamState != 1)
                        {
                            Client.SendWhisper("El camión no ha sido cargado aún.", 1);
                            return;
                        }
                        if (VO[0].CamState == 2)
                        {
                            Client.SendWhisper("El camión ya ha sido descargado. ¡Ve a entregarlo a Camioneros! ((Usa :entregarcamion))", 1);
                            return;
                        }
                        if (VO[0].CamDest != Room.Id)
                        {
                            if (RoleplayManager.GenerateRoom(VO[0].CamDest, out Room _room))
                                Client.SendWhisper("¡Debes ir a " + _room.Name + " para entregar la mercancía!", 1);
                            return;
                        }

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes estar en la zona de descarga de tu destino para entregar la mercancía.", 1);
                            return;
                        }
                        #endregion

                        if (Client.GetRoleplay().IsCamUnLoading)
                        {
                            Client.SendWhisper("Ya te encuentras descargando el camión. Por favor espera...", 1);
                            return;
                        }
                        #endregion

                        #region Execute
                        Client.GetRoleplay().IsCamUnLoading = true;
                        Client.GetRoleplay().LoadingTimeLeft = RoleplayManager.CamDepositTime;

                        RoleplayManager.Shout(Client, "*Comienza a descargar su camión*", 5);
                        Client.SendWhisper("Debes esperar " + Client.GetRoleplay().LoadingTimeLeft + " segundo(s)...", 1);
                        Client.GetRoleplay().TimerManager.CreateTimer("general", 1000, true);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
                        #endregion
                    }
                    break;
                #endregion

                #region Entregar
                case "entregar":
                    {
                        #region Conditions
                        if (Client.GetRoleplay().TryGetCooldown("cargcam"))
                            return;

                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;
                        #endregion

                        #region Group Conditions
                        List<Groups.Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Client.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Client.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Client.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 0; // Desempleado
                            Client.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Client.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "camionero"))
                        {
                            Client.SendWhisper("Debes tener el trabajo de Camionero para usar ese comando.", 1);
                            return;
                        }
                        if (!Client.GetRoleplay().DrivingCar)
                        {
                            Client.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                            return;
                        }

                        #region Get Information form VehiclesManager
                        Vehicle vehicle = null;
                        int corp = 0;
                        foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                        {
                            if (Client.GetRoleplay().CarEffectId == Vehicle.EffectID)
                            {
                                vehicle = Vehicle;
                                corp = Convert.ToInt32(Vehicle.CarCorp);
                            }
                        }
                        if (vehicle == null)
                        {
                            Client.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                            return;
                        }
                        #endregion

                        if (!Client.GetRoleplay().DrivingCar || !GroupManager.GetJob(corp).Name.Contains("Camioneros"))
                        {
                            Client.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                            return;
                        }
                        #endregion

                        #region Camionero Conditions
                        List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Client.GetRoleplay().DrivingCarId);
                        if (VO == null || VO.Count <= 0)
                        {
                            Client.SendWhisper("((No se pudo obtener información del vehículo que conduces))", 1);
                            return;
                        }
                        if (VO[0].CamOwnId > 0 && VO[0].CamOwnId != Client.GetHabbo().Id)
                        {
                            Client.SendWhisper("Este camión pertenece a por otra persona. ((Si perdiste el tuyo usa :abandonarcarga))", 1);
                            return;
                        }
                        if (VO[0].CamState == 0)
                        {
                            Client.SendWhisper("El camión no ha sido cargado aún. ¡Ve a cargarlo de mercancía! ((Usa :cargarcamion [ID]))", 1);
                            return;
                        }
                        if (VO[0].CamState != 2)
                        {
                            Client.SendWhisper("El camión no ha sido descargado aún. ¡Ve a entregar la mercancía!", 1);
                            return;
                        }
                        if (VO[0].CamDest != Room.Id)
                        {
                            if(RoleplayManager.GenerateRoom(VO[0].CamDest, out Room _room))
                                Client.SendWhisper("¡Debes ir a " + _room.Name + " para entregar el camión!", 1);
                            return;
                        }

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carr2" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes estar en la zona de entrega para terminar el recorrido.", 1);
                            return;
                        }
                        #endregion

                        #endregion

                        #region Execute

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

                                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                            }
                        }

                        // CHOFER 
                        Client.GetRoleplay().PasajerosCount = 0;
                        Client.GetRoleplay().Pasajeros = "";
                        Client.GetRoleplay().Chofer = false;
                        Client.GetRoomUser().AllowOverride = false;
                        #endregion

                        List<Groups.Group> MyGang = PolarEnvironment.GetGame().GetGroupManager().GetGangsForUser(Client.GetHabbo().Id);

                        #region Pagas
                        int Amn = 0, Med = 0, Crack = 0, Piezas = 0, bonif = 0;

                        #region Cants By Level
                        if (Client.GetRoleplay().CamLvl == 1)
                        {
                            Amn = 13;
                            Med = 1;
                            Crack = 1;
                            Piezas = 1;
                        }
                        else if (Client.GetRoleplay().CamLvl == 2)
                        {
                            Amn = 16;
                            Med = 3;
                            Crack = 2;
                            Piezas = 2;
                        }
                        else if (Client.GetRoleplay().CamLvl == 3)
                        {
                            Amn = 20;
                            Med = 4;
                            Crack = 3;
                            Piezas = 4;
                        }
                        else if (Client.GetRoleplay().CamLvl == 4)
                        {
                            Amn = 22;
                            Med = 5;
                            Crack = 4;
                            Piezas = 5;
                        }
                        else if (Client.GetRoleplay().CamLvl == 5)
                        {
                            Amn = 25;
                            Med = 7;
                            Crack = 5;
                            Piezas = 5;
                        }
                        else if (Client.GetRoleplay().CamLvl >= 6) // Max Lvl
                        {
                            Amn = 30;
                            Med = 10;
                            Crack = 6;
                            Piezas = 7;
                        }
                        #endregion

                        string win = "¡Excelente entrega! Tus ganancias son: $" + Amn;
                        #region By Carg
                        if (VO[0].CamCargId == 3)// Drogas
                        {
                            Amn = 0;
                            Client.GetRoleplay().Cocaine += Crack;
                            Client.GetRoleplay().Medicina += Med;
                            //RoleplayManager.SaveQuickStat(Client, "cocaine", "" + Client.GetRoleplay().Cocaine);
                            //RoleplayManager.SaveQuickStat(Client, "medicines", "" + Client.GetRoleplay().Medicines);
                            win = "¡Excelente entrega! Tus ganancias son: " + Med + " Medicamentos + " + Crack + "g de Crack.";

                        }

                        Client.GetHabbo().Credits += Amn; 
                        Client.GetHabbo().UpdateCreditsBalance();
                        
                        #endregion
                        #endregion

                        // Reseteamos Camion por seguridad
                        VO[0].CamCargId = 0;
                        VO[0].CamDest = 0;
                        VO[0].CamOwnId = 0;
                        VO[0].CamState = 0;

                        // Quitar Camión
                        PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetRoleplay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);

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

                        Client.GetRoleplay().CamCargId = 0;// Retornamos a 0 para que pueda cargar otro

                        RoleplayManager.Shout(Client, "*Entrega su camión completando su recorrido*", 5);
                        Client.SendWhisper(win, 1);
                        RoleplayManager.JobSkills(Client, Client.GetRoleplay().JobId, Client.GetRoleplay().CamLvl, Client.GetRoleplay().CamXP);


                        if (MyGang != null && MyGang.Count > 0)
                        {
                            if (MyGang[0].BankRuptcy)
                            {
                                Client.SendWhisper("Tu banda está en bancarota y no podrás gozar de los beneficios de ella.", 1);
                            }
                            else if (bonif > 0)
                            {
                                MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " ha obtenido $ " + String.Format("{0:N0}", bonif) + " para la banda en cargas ilegales.", bonif);
                                Client.SendWhisper("¡Tu banda y tú han ganado una bonifcación extra de $ " + String.Format("{0:N0}", bonif) + " por tu entrega ilegal!", 1);
                            }
                        }

                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "close");// WS FUEL
                        Client.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
                        #endregion
                    }
                    break;
                #endregion

                #region Abandonar
                case "abandonar":
                    {
                        #region Conditions
                        if (Client.GetRoleplay().TryGetCooldown("cargcam"))
                            return;
                        #endregion

                        #region Group Conditions
                        List<Groups.Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Client.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Client.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Client.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetRoleplay().JobId = Groups[GroupNumber].Id;
                        Client.GetRoleplay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!GroupManager.JobExists(Client.GetRoleplay().JobId, Client.GetRoleplay().JobRank))
                        {
                            Client.GetRoleplay().TimeWorked = 0;
                            Client.GetRoleplay().JobId = 0; // Desempleado
                            Client.GetRoleplay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Client.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                            return;
                        }

                        if (!GroupManager.HasJobCommand(Client, "camionero") && !GroupManager.HasJobCommand(Client, "basurero"))
                        {
                            Client.SendWhisper("Debes tener el trabajo de Camionero o Basurero para usar ese comando.", 1);
                            return;
                        }
                        /*
                        if (Client.GetRoleplay().DrivingCar)
                        {
                            Client.SendWhisper("No puedes hacer eso mientras conduces.", 1);
                            return;
                        }
                        */
                        #endregion

                        #region Camionero Conditions
                        if (GroupManager.HasJobCommand(Client, "camionero"))
                        {
                            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByCamOwnId(Client.GetHabbo().Id);
                            if (VO == null || VO.Count <= 0)
                            {
                                Client.SendWhisper("No tienes ninguna carga a tu nombre para abandonar.", 1);
                                return;
                            }
                            else
                            {
                                // Quitar Camión de Diccionario
                                PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(VO[0].Id);
                                Client.GetRoleplay().CamCargId = 0;
                            }
                        }
                        else
                        {
                            if (!Client.GetRoleplay().IsBasuChofer)
                            {
                                Client.SendWhisper("No eres el chofer de ninguna carga de basura a abandonar.", 1);
                                return;
                            }
                        }
                        #endregion

                        #region Execute
                        RoleplayManager.Shout(Client, "*Abandona la Carga de su Camión*", 5);

                        if (!Client.GetRoleplay().IsBasuChofer)
                            Client.SendWhisper("Tu Camión ha sido descargadado. No has terminado el recorrido, no se te pagará nada.", 1);
                        else
                        {
                            Client.GetRoleplay().IsBasuChofer = false;

                            // Solo al abandonar carga
                            Client.GetRoleplay().BasuTeamId = 0;
                            Client.GetRoleplay().BasuTeamName = string.Empty;
                            Client.GetRoleplay().BasuTrashCount = 0;

                            Client.SendWhisper("Su Camión ha sido abandonado. No han terminado el recorrido, no se les pagará nada.", 1);
                        }

                        RoleplayManager.CheckCorpCarp(Client);

                        #region Driving
                        if (Client.GetRoleplay().DrivingCar)
                        {
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
                                    {
                                        PJ.GetRoleplay().IsBasuPasaj = false;

                                        // Solo al abandonar carga
                                        PJ.GetRoleplay().BasuTeamId = 0;
                                        PJ.GetRoleplay().BasuTeamName = string.Empty;
                                        PJ.GetRoleplay().BasuTrashCount = 0;
                                        PJ.SendWhisper("Su Camión ha sido abandonado. No han terminado el recorrido, no se les pagará nada.", 1);
                                    }

                                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                                }
                            }

                            // CHOFER 
                            Client.GetRoleplay().PasajerosCount = 0;
                            Client.GetRoleplay().Pasajeros = "";
                            Client.GetRoleplay().Chofer = false;

                            if (Client.GetRoomUser() != null)
                                Client.GetRoomUser().AllowOverride = false;
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

                            if (Client.GetRoomUser() != null)
                            {
                                Client.GetRoomUser().ApplyEffect(0);
                                Client.GetRoomUser().FastWalking = false;
                            }
                            #endregion

                            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_vehicle", "close");// WS FUEL
                        }
                        #endregion

                        Client.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
                        #endregion
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().ViewCamCargas = false;
                        Socket.SendWS( "compose_camionero|close|");
                    }
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
