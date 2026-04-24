using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using System.Drawing;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class UpCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_driving_up"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Permite subir de pasajero a un auto que alguien conduzca."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el nombre del chofer. :subir [usuario]", 1);
                return;
            }
            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Ha ocurrido un error en buscar a la persona, probablemente esté desconectada.", 1);
                return;
            }
            /*if (Session.GetHabbo().EscortID > 0)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás siendo escoltad@", 1);
                return;
            }
            if (Session.GetHabbo().TaxiChofer > 0)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás vas en Taxi", 1);
                return;
            }
            if (Session.GetHabbo().Escorting > 0)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás escoltas a alguien", 1);
                return;
            }*/
            if (Session.GetRoleplay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (!Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para poder subir al vehículo.", 1);
                return;
            }
            if (Session.GetRoleplay().Pasajero)
            {
                Session.SendWhisper("¡Ya vas de pasajero de alguien!", 1);
                return;
            }
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            if (TargetClient == Session)
            {
                Session.SendWhisper("No puedes ir de pasajer@ de ti mism@.", 1);
                return;
            }
            if (Session.GetRoleplay().DrivingInCar || Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }      
            if (Session.GetRoomUser().IsWalking)
			{
                Session.SendWhisper("¡No puedes hacer eso mientras caminas!", 1);
                return;
            }
            if (!TargetClient.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("¡Esa persona no se encuentra conduciendo!", 1);
                return;
            }
            if (TargetClient.GetRoomUser().IsWalking)
            {
                Session.SendWhisper("¡No subirte a un vehículo en movimiento!", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetClient.GetRoomUser().X, TargetClient.GetRoomUser().Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance > 1)
            {
                Session.SendWhisper("¡Debes acercarte más al chofer!", 1);
                return;
            }
            if (Session.GetRoleplay().TryGetCooldown("pasajero"))
                return;
            #endregion

            #region Execute            
            VehiclesOwned VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwned(TargetClient.GetRoleplay().DrivingCarId);
            if(VO == null)
            {
                Session.SendWhisper("No se pudo obtener información del vehículo de " + TargetClient.GetHabbo().Username, 1);
                return;
            }

            #region Get Veihcle Info (play_vehicles)  
            Vehicle vehicle = null;
            bool found = false;
            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
            {
                if (!found)
                {
                    if (Vehicle.DisplayName == VO.Model)
                    {
                        found = true;
                        vehicle = Vehicle;
                    }
                }
            }
            //Al examinar todos los autos ninguno conincide con el item donde está parado el user...
            if (!found || vehicle == null)
            {
                Session.SendWhisper("No se pudo obtener información del vehículo que " + TargetClient.GetHabbo().Username + " conduce.", 1);
                return;
            }
            #endregion

            if (TargetClient.GetRoleplay().PasajerosCount >= vehicle.MaxDoors)
            {
                Session.SendWhisper("No hay espacio suficiente para subir de pasajero en ese vehículo.", 1);
                return;
            }

            if(VO.State == 1 || VO.State == 3)
            {
                Session.SendWhisper("Las puertas del Vehículo están cerradas.", 1);
                return;
            }

            // PASAJERO
            Session.GetRoomUser().ClearMovement(true);
            Session.GetRoleplay().Pasajero = true;
            Session.GetRoleplay().ChoferName = TargetClient.GetHabbo().Username;
            Session.GetRoleplay().ChoferClient = TargetClient;
            Session.GetRoleplay().ChoferID = TargetClient.GetHabbo().Id;
            Session.GetRoomUser().CanWalk = false;
            Session.GetRoomUser().FastWalking = true;
            Session.GetRoomUser().TeleportEnabled = true;
            Session.GetRoomUser().AllowOverride = true;
            //Session.GetRoleplay().Invisible = true;
            Session.SendMessage(new UserRemoveComposer(Session.GetRoomUser().VirtualId));

            // CHOFER 
            TargetClient.GetRoleplay().Chofer = true;
            TargetClient.GetRoleplay().Pasajeros += Session.GetHabbo().Username + ";";
            TargetClient.GetRoleplay().PasajerosCount++;
            TargetClient.GetRoomUser().FastWalking = true;
            TargetClient.GetRoomUser().AllowOverride = true;
            TargetClient.SendMessage(new UserRemoveComposer(Session.GetRoomUser().VirtualId));

            //Animación de subir al Auto
            int NewX = TargetClient.GetRoomUser().X;
            int NewY = TargetClient.GetRoomUser().Y;
            Room.SendMessage(Room.GetRoomItemHandler().UpdateUserOnRoller(Session.GetRoomUser(), new Point(NewX, NewY), 0, Room.GetGameMap().SqAbsoluteHeight(NewX, NewY)));
            Session.GetRoomUser().MoveTo(NewX, NewY);


            RoleplayManager.Shout(Session, "*Sube al vehículo de " + TargetClient.GetHabbo().Username + "*", 5);
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "open");// WS FUEL
            Session.GetRoleplay().CooldownManager.CreateCooldown("pasajero", 1000, 5);

            #region Basurero
            if (TargetClient.GetRoleplay().IsBasuChofer)
            {
                if (Session.GetRoleplay().IsBasuChofer && Session.GetRoleplay().BasuTeamId > 0)
                {
                    Session.SendWhisper("¡No puedes ser recolector de otro chofer! Primero abandona la carga actual.", 1);
                    return;
                }

                if (TargetClient.GetRoleplay().BasuTeamId <= 0)
                {
                    // Asignamos de compañero al que subió siempre y cuando sea basurero también.
                    if (GroupManager.HasJobCommand(Session, "basurero"))
                    {
                        /*if (Session.GetHabbo().VIPRank < 1)
                        {
                            if (!Session.GetRoleplay().IsWorking)
                            {
                                Session.SendWhisper("¡Debes tener el uniforme de Basurero para asignarte de compañero a ese chofer!", 1);
                                return;
                            }
                        }*/

                        Session.GetRoleplay().IsBasuPasaj = true;
                        // Sincronizamos Team
                        TargetClient.GetRoleplay().BasuTeamId = Session.GetHabbo().Id;
                        TargetClient.GetRoleplay().BasuTeamName = Session.GetHabbo().Username;
                        Session.GetRoleplay().BasuTeamId = TargetClient.GetHabbo().Id;
                        Session.GetRoleplay().BasuTeamName = TargetClient.GetHabbo().Username;
                        Session.SendWhisper("Se te ha asignado a " + TargetClient.GetHabbo().Username + " como tu Compañero de Basurero. ¡Pueden comenzar!", 1);
                        TargetClient.SendWhisper("Se te ha asignado a " + Session.GetHabbo().Username + " como tu Compañero de Basurero. ¡Pueden comenzar!", 1);

                        PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(TargetClient,
                            "compose_basurero|" +
                            "showinfo|" +
                            TargetClient.GetHabbo().Username + "|" + // Chofer
                            Session.GetHabbo().Username + "|" + // Recolector
                            TargetClient.GetRoleplay().BasuTrashCount + "/15|" +
                            TargetClient.GetRoleplay().IsBasuChofer);
                    }
                }
                else
                {
                    if (GroupManager.HasJobCommand(Session, "basurero"))
                        Session.SendWhisper("Esta persona ya tiene un compañero de trabajo asignado.", 1);
                }

                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session,
                            "compose_basurero|" +
                            "showinfo|" +
                            TargetClient.GetHabbo().Username + "|" + // Chofer
                            Session.GetHabbo().Username + "|" + // Recolector
                            Session.GetRoleplay().BasuTrashCount + "/15|" +
                            Session.GetRoleplay().IsBasuChofer);
            }
            #endregion
            #endregion
        }
    }
}
