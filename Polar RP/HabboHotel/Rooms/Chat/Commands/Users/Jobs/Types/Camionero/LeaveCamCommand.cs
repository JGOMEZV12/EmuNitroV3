using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboRoleplay.VehicleOwned;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank
{
    class LeaveCamCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_leave_camion"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Siendo Camionero, permite abandonar tu carga para poder usar un nuevo camión."; }
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

            if (!GroupManager.HasJobCommand(Session, "camionero") && !GroupManager.HasJobCommand(Session, "basurero"))
            {
                Session.SendWhisper("Debes tener el trabajo de Camionero o Basurero para usar ese comando.", 1);
                return;
            }
            /*
            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("No puedes hacer eso mientras conduces.", 1);
                return;
            }
            */
            #endregion

            #region Camionero Conditions
            if (GroupManager.HasJobCommand(Session, "camionero"))
            {
                List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByCamOwnId(Session.GetHabbo().Id);
                if (VO == null || VO.Count <= 0)
                {
                    Session.SendWhisper("No tienes ninguna carga a tu nombre para abandonar.", 1);
                    return;
                }
                else
                {
                    // Quitar Camión de Diccionario
                    PolarEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(VO[0].Id);
                    Session.GetRoleplay().CamCargId = 0;
                }
            }
            else
            {
                if (!Session.GetRoleplay().IsBasuChofer)
                {
                    Session.SendWhisper("No eres el chofer de ninguna carga de basura a abandonar.", 1);
                    return;
                }
            }
            #endregion

            #region Execute
            RoleplayManager.Shout(Session, "*Abandona la Carga de su Camión*", 5);
           
            if (!Session.GetRoleplay().IsBasuChofer)
                Session.SendWhisper("Tu Camión ha sido descargadado. No has terminado el recorrido, no se te pagará nada.", 1);
            else
            {
                Session.GetRoleplay().IsBasuChofer = false;

                // Solo al abandonar carga
                Session.GetRoleplay().BasuTeamId = 0;
                Session.GetRoleplay().BasuTeamName = string.Empty;
                Session.GetRoleplay().BasuTrashCount = 0;

                Session.SendWhisper("Su Camión ha sido abandonado. No han terminado el recorrido, no se les pagará nada.", 1);
            }

            RoleplayManager.CheckCorpCarp(Session);

            #region Driving
            if (Session.GetRoleplay().DrivingCar)
            {
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
                Session.GetRoleplay().PasajerosCount = 0;
                Session.GetRoleplay().Pasajeros = "";
                Session.GetRoleplay().Chofer = false;
                Session.GetRoomUser().AllowOverride = false;
                #endregion

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

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "close");// WS FUEL
            }
            #endregion

            Session.GetRoleplay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
            #endregion
        }
    }
}
