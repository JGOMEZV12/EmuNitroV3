using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Vehicles;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Basurero
{
    class ReturnBasuCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_return_basurero"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Siendo el conductor del camión de basura, descarga la basura para recibir tu paga."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().TryGetCooldown("cargcam"))
                return;
            #endregion

            #region Group Conditions
            List<Group> Groups = PolarEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

            if (Groups.Count <= 0)
            {
                Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                return;
            }

            int GroupNumber = -1;

            if (Groups[0].GType != 2 && Groups[0].Id >= 2)
            {
                if (Groups.Count > 1)
                {
                    if (Groups[1].GType != 2 && Groups[1].Id >= 2)
                    {
                        Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                        return;
                    }
                    GroupNumber = 1; // Segundo indicie de variable
                }
                else
                {
                    Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                    return;
                }
            }
            else
            {
                GroupNumber = 0; // Primer indice de Variable Group
            }

            Session.GetRoleplay().JobId = Groups[GroupNumber].Id;
            Session.GetRoleplay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
            #endregion

            #region Extra Conditions            
            // Existe el trabajo?
            if (!GroupManager.JobExists(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank))
            {
                Session.GetRoleplay().TimeWorked = 0;
                Session.GetRoleplay().JobId = 1; // Desempleado
                Session.GetRoleplay().JobRank = 1;

                //Room.Group.DeleteMember(Session.GetHabbo().Id);// OJO ACÁ

                Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "basurero"))
            {
                Session.SendWhisper("Debes tener el trabajo de Basurero para usar ese comando.", 1);
                return;
            }

            #endregion

            #region Basurero Conditions
            string MyCity = Room.City;
            RPRoom Data;
            int Basurero = PolarEnvironment.GetGame().GetRPRoomManager().TryToGetBasureros(MyCity, out Data);
            if (Room.Id != Basurero)
            {
                Session.SendWhisper("¡Debes ir al Basurero de la Ciudad para descargar el camión!", 1);
                return;
            }
            /*if (Session.GetRoleplay().BasuTeamId <= 0)
            {
                Session.SendWhisper("¡Primero debes conseguir un compañero de trabajo para recolectar 15 contenedores de basura!", 1);
                return;
            }*/
            if (Session.GetRoleplay().DrivingInCar)
            {
                Session.SendWhisper("¡Debes estar conduciendo el camión de basura!", 1);
                return;
            }
            #region Get Information form VehiclesManager
            Vehicle vehicle = null;
            int corp = 0;
            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
            {
                if (Session.GetRoleplay().CarEffectId == Vehicle.EffectID)
                {
                    vehicle = Vehicle;
                    corp = Convert.ToInt32(Vehicle.CarCorp);
                }
            }
            if (vehicle == null)
            {
                Session.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                return;
            }
            #endregion

            if (corp != Session.GetRoleplay().JobId)
            {
                Session.SendWhisper("¡Debes estar conduciendo el camión de basura!", 1);
                return;
            }
            if (!Session.GetRoleplay().IsBasuChofer)
            {
                Session.SendWhisper("¡Solo el chofer del Camión puede hacer eso!", 1);
                return;
            }
            if (Session.GetRoleplay().BasuTrashCount < 15)
            {
                Session.SendWhisper("¡Deben recolectar 15 contedendores de basura para poder descargar el camión!", 1);
                return;
            }
            /*GameClient TeamPasaj = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Session.GetRoleplay().BasuTeamId);
            if (TeamPasaj == null)
            {
                Session.SendWhisper("Al parecer tu compañero de Basurero se ha ido y han Fracasado el Recorrido.", 1);

                // Quitar Camión
                PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Session.GetRoleplay().DrivingCarId);
                #region Online ParkVars
                //Retornamos a valores predeterminados
                Session.GetRoleplay().DrivingCar = false;
                Session.GetRoleplay().DrivingInCar = false;
                Session.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

                //Combustible System
                Session.GetRoleplay().CarType = 0;// Define el gasto de combustible
                Session.GetRoleplay().CarFuel = 0;
                Session.GetRoleplay().CarMaxFuel = 0;
                Session.GetRoleplay().CarTimer = 0;
                Session.GetRoleplay().CarLife = 0;

                Session.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
                Session.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                Session.GetRoomUser().ApplyEffect(0);
                Session.GetRoomUser().FastWalking = false;
                #endregion

                #region Retornamos Basurero Vars
                Session.GetRoleplay().BasuTeamId = 0;
                Session.GetRoleplay().BasuTeamName = "";
                Session.GetRoleplay().BasuTrashCount = 0;
                Session.GetRoleplay().IsBasuPasaj = false;
                Session.GetRoleplay().IsBasuChofer = false;
                #endregion
                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");// WS FUEL
                return;
            }
            if (!Session.GetRoleplay().Pasajeros.Contains(TeamPasaj.GetHabbo().Username))
            {
                Session.SendWhisper("Tu compañero de Basurero debe estar de Pasajero de tu Camión.", 1);
                return;
            }*/

            #region Comodin Conditions
            Item BTile = null;
            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
            if (BTile == null)
            {
                Session.SendWhisper("Debes estar en cerca del bulto de basura central para terminar el recorrido.", 1);
                return;
            }
            #endregion

            #endregion

            #region Execute

            #region Pagas
            int PayC = (Session.GetRoleplay().BasuLvl * 25);
            //int PayP = (TeamPasaj.GetRoleplay().BasuLvl * 25);
            #endregion

            // Quitar Camión
            PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Session.GetRoleplay().DrivingCarId);
            RoleplayManager.CheckCorpCarp(Session);

            #region Online ParkVars
            //Retornamos a valores predeterminados
            Session.GetRoleplay().DrivingCar = false;
            Session.GetRoleplay().DrivingInCar = false;
            Session.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

            //Combustible System
            Session.GetRoleplay().CarType = 0;// Define el gasto de combustible
            Session.GetRoleplay().CarFuel = 0;
            Session.GetRoleplay().CarMaxFuel = 0;
            Session.GetRoleplay().CarTimer = 0;
            Session.GetRoleplay().CarLife = 0;

            Session.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
            Session.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
            Session.GetRoomUser().ApplyEffect(0);
            Session.GetRoomUser().FastWalking = false;
            #endregion

            #region Retornamos Basurero Vars
            Session.GetRoleplay().BasuTeamId = 0;
            Session.GetRoleplay().BasuTeamName = "";
            Session.GetRoleplay().BasuTrashCount = 0;
            Session.GetRoleplay().IsBasuPasaj = false;
            Session.GetRoleplay().IsBasuChofer = false;
/*
            TeamPasaj.GetRoleplay().BasuTeamId = 0;
            TeamPasaj.GetRoleplay().BasuTeamName = "";
            TeamPasaj.GetRoleplay().BasuTrashCount = 0;
            TeamPasaj.GetRoleplay().IsBasuPasaj = false;
            TeamPasaj.GetRoleplay().IsBasuChofer = false;
            #endregion

            #region Online ParkVars & Pasajero TeamPasaj
            //Retornamos a valores predeterminados
            TeamPasaj.GetRoleplay().DrivingCar = false;
            TeamPasaj.GetRoleplay().DrivingInCar = false;
            TeamPasaj.GetRoleplay().DrivingCarId = 0;// Id de VehiclesOwned;

            //Combustible System
            TeamPasaj.GetRoleplay().CarType = 0;// Define el gasto de combustible
            TeamPasaj.GetRoleplay().CarFuel = 0;
            TeamPasaj.GetRoleplay().CarMaxFuel = 0;
            TeamPasaj.GetRoleplay().CarTimer = 0;
            TeamPasaj.GetRoleplay().CarLife = 0;

            TeamPasaj.GetRoleplay().CarEnableId = 0;//Coloca el enable para conducir
            TeamPasaj.GetRoleplay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
            if(TeamPasaj.GetRoomUser() != null)
                TeamPasaj.GetRoomUser().ApplyEffect(0);
            TeamPasaj.GetRoomUser().FastWalking = false;

            // PASAJERO
            TeamPasaj.GetRoleplay().Pasajero = false;
            TeamPasaj.GetRoleplay().ChoferName = "";
            TeamPasaj.GetRoleplay().ChoferID = 0;
            if (TeamPasaj.GetRoomUser() != null)
            {
                TeamPasaj.GetRoomUser().CanWalk = true;
                TeamPasaj.GetRoomUser().FastWalking = false;
                TeamPasaj.GetRoomUser().TeleportEnabled = false;
                TeamPasaj.GetRoomUser().AllowOverride = false;
            }*/

            // CHOFER 
            // Descontamos Pasajero
            Session.GetRoleplay().PasajerosCount = 0;
            Session.GetRoleplay().Pasajeros = "";            
            Session.GetRoleplay().Chofer = false;
            Session.GetRoomUser().AllowOverride = false;
            #endregion
            /*
            #region Retornamos Basurero Vars TeamPasaj
            TeamPasaj.GetRoleplay().BasuTeamId = 0;
            TeamPasaj.GetRoleplay().BasuTeamName = "";
            TeamPasaj.GetRoleplay().BasuTrashCount = 0;
            TeamPasaj.GetRoleplay().IsBasuPasaj = false;
            TeamPasaj.GetRoleplay().IsBasuChofer = false;

            TeamPasaj.GetRoleplay().BasuTeamId = 0;
            TeamPasaj.GetRoleplay().BasuTeamName = "";
            TeamPasaj.GetRoleplay().BasuTrashCount = 0;
            TeamPasaj.GetRoleplay().IsBasuPasaj = false;
            TeamPasaj.GetRoleplay().IsBasuChofer = false;
            #endregion*/

            #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
            //Vars
            string Pasajeros = Session.GetRoleplay().Pasajeros;
            string[] stringSeparators = new string[] { ";" };
            string[] result;
            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string psjs in result)
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

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(PJ, "event_vehicle", "close");// WS FUEL
                }
            }

            // CHOFER 
            Session.GetRoleplay().PasajerosCount = 0;
            Session.GetRoleplay().Pasajeros = "";
            Session.GetRoleplay().Chofer = false;
            Session.GetRoomUser().AllowOverride = false;
            #endregion

            RoleplayManager.Shout(Session, "*Descarga el camión de basura completando su trabajo*", 5);
            Session.SendWhisper("¡Buen trabajo! Tus ganancias son: " + PayC, 1);
            //TeamPasaj.SendWhisper("¡Buen trabajo! Tus ganancias son: " + PayP, 1);

            Session.GetHabbo().Credits += PayC;
            Session.GetHabbo().UpdateCreditsBalance();
            //TeamPasaj.GetHabbo().Credits += PayP;
            //TeamPasaj.GetHabbo().UpdateCreditsBalance();

            RoleplayManager.JobSkills(Session, Session.GetRoleplay().JobId, Session.GetRoleplay().BasuLvl, Session.GetRoleplay().BasuXP);
            //RoleplayManager.JobSkills(TeamPasaj, TeamPasaj.GetRoleplay().JobId, TeamPasaj.GetRoleplay().BasuLvl, TeamPasaj.GetRoleplay().BasuXP);
            
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");// WS FUEL
            Session.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
            return;
            #endregion
        }
    }
}
