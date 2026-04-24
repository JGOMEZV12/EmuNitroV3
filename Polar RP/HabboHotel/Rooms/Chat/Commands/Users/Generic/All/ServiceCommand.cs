using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.RPRoom;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Bots;
using System.Threading;
using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Catalog.Pets;
using Polar.HabboHotel.Items.Utilities;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class ServiceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_service_jobs"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Pide el servicio de grua, médico, mecánico o armero."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el tipo de servicio. (medico, mecanico, grua).", 1);
                return;
            }

            /*if (RoleplayManager.PurgeEvent)
            {
                Session.SendWhisper("¡Todos los servicios no están disponibles durante la purga!", 1);
                return;
            }*/
            #endregion

            #region Execute
            string Serv = Convert.ToString(Params[1]);
            int roomid = 0;
            Room ThisRoom = null;
            switch (Serv)
            {
                #region Médico
                case "medico":
                    #region Conditions
                    if (Session.GetRoleplay().TryGetCooldown("servmedico", true))
                        return;
                    #endregion

                    #region Execute
                    roomid = Session.GetRoomUser().RoomId;
                    if (!RoleplayManager.GenerateRoom(roomid, out ThisRoom, false))
                    {
                        Session.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                        return;
                    }
                    RoleplayManager.Shout(Session, "*Llamó a un paramédico y espera*", 5);
                    Session.SendWhisper("Has mandado una solicitud de Ambulancia. Un Médico atenderá tu llamado. Por favor espera.", 1);
                    Session.GetRoomUser().ApplyEffect(599);
                    Session.GetRoleplay().PediMedico = true;
                    PolarEnvironment.GetGame().GetClientManager().sendWorkAlert(Session.GetHabbo().Username + " ha solicitado servicio de médico en "+ThisRoom.Name, "heal", true);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("servmedico", 5000, 10);
                    #endregion
                    break;
                #endregion

                #region Armero
                case "armero":
                    #region Basic Conditions
                    if (Session.GetRoleplay().EquippedWeapon == null)
                    {
                        Session.SendWhisper("Debes equipar un arma para solicitar los servicios de un Armero.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().Cuffed)
                    {
                        Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                        return;
                    }
                    if (!Session.GetRoomUser().CanWalk)
                    {
                        Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().Pasajero)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
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

                    if (Session.GetRoleplay().DrivingCar)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().TryGetCooldown("servarm", true))
                    {
                        Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                        return;
                    }
                    #endregion

                    #region Execute
                    roomid = Session.GetRoomUser().RoomId;
                    if (!RoleplayManager.GenerateRoom(roomid, out ThisRoom, false))
                    {
                        Session.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                        return;
                    }
                    RoleplayManager.Shout(Session, "*Ha publicado una solicitud de Armero*", 5);
                    Session.SendWhisper("Has mandado una solicitud de Armero. Un Armero atenderá tu llamado. Por favor espera.", 1);
                    Session.GetRoleplay().PediArm = true;
                    PolarEnvironment.GetGame().GetClientManager().sendWorkAlert(Session.GetHabbo().Username + " ha solicitado servicio de Armero en " + ThisRoom.Name, "armero");
                    Session.GetRoleplay().CooldownManager.CreateCooldown("servarm", 5000, 10);
                    #endregion
                    break;
                #endregion

                #region Grúa
                case "grua":
                    #region Basic Conditions
                    if (Session.GetRoleplay().Cuffed)
                    {
                        Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                        return;
                    }
                    if (!Session.GetRoomUser().CanWalk)
                    {
                        Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().Pasajero)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
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
                    if (Session.GetRoleplay().DrivingCar)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().TryGetCooldown("grua", true))
                    {
                        Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                        return;
                    }
                    #endregion
                    /*
                    if (!Room.MunicipalidadEnabled)
                    {
                        Session.SendWhisper("Debes estar en la municpalidad para pedir ese servicio.", 1);
                        return;
                    }*/

                    #region Action Point Conditions
                    /*Item BTile = null;
                    BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Session.GetRoomUser().Coordinate);
                    if (BTile == null)
                    {
                        Session.SendWhisper("Debes acercarte al mostrador para solicitar el servicio de grúa.", 1);
                        return;
                    }*/
                    #endregion

                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "close");
                    PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_products", "open_grua");
                    break;
                #endregion

                #region Taxi OFF
                    /*
                case "taxi":
                    #region Basic Conditions
                    
                    if (Session.GetRoleplay().Cuffed)
                    {
                        Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                        return;
                    }
                    if (!Session.GetRoomUser().CanWalk)
                    {
                        Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().Pasajero)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
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
                    if (Session.GetRoleplay().IsDying)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().DrivingCar)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().TryGetCooldown("servtaxi", true))
                    {
                        Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                        return;
                    }
                    #endregion

                    #region Execute
                    roomid = Session.GetRoomUser().RoomId;
                    RoleplayManager.GenerateRoom(roomid, out ThisRoom, false);
                    if (ThisRoom == null)
                    {
                        Session.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                        return;
                    }
                    RoleplayManager.Shout(Session, "*Ha solicitado un Taxi*", 5);
                    Session.SendWhisper("Has mandado una solicitud de Taxi. Un Taxista atenderá tu llamado. Por favor espera.", 1);
                    PolarEnvironment.GetGame().GetClientManager().sendWorkAlert(Session.GetHabbo().Username + " ha solicitado un taxi en " + ThisRoom.Name, "taxi");
                    Session.GetRoleplay().CooldownManager.CreateCooldown("sertaxi", 1000, 30);
                    #endregion
                    break;
                    */
                #endregion

                #region Mecánico
                case "mecanico":
                    #region Basic Conditions

                    if (Session.GetRoleplay().Cuffed)
                    {
                        Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                        return;
                    }
                    if (!Session.GetRoomUser().CanWalk)
                    {
                        Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                        return;
                    }
                    if (Session.GetRoleplay().Pasajero)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
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

                    if (Session.GetRoleplay().DrivingCar)
                    {
                        Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                        return;
                    }
                    if (Session.GetRoleplay().TryGetCooldown("servmeca", true))
                    {
                        Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                        return;
                    }
                    #endregion

                    #region Execute
                    roomid = Session.GetRoomUser().RoomId;
                    if (!RoleplayManager.GenerateRoom(roomid, out ThisRoom, false))
                    {
                        Session.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [2]", 1);
                        return;
                    }

                    /*
                    Task.Run(async delegate
                    {
                        await Task.Delay(5000);
                        RoleplayBotManager.DeployBotByID(35, "default", Room.Id);
                        //RoleplayManager.CalledDelivery = false;
                    });*/

                    RoleplayManager.Shout(Session, "*Ha solicitado un Mecánico*", 5);
                    Session.SendWhisper("Has mandado una solicitud de Mécánico. Un Mecánico atenderá tu llamado. Por favor espera.", 1);
                    Session.GetRoleplay().PediMec = true;
                    PolarEnvironment.GetGame().GetClientManager().sendWorkAlert(Session.GetHabbo().Username + " ha solicitado un mecánico en " + ThisRoom.Name, "mecanico", true, Room.Id);
                    Session.GetRoleplay().CooldownManager.CreateCooldown("servmeca", 5000, 10);

                    #endregion
                    break;
                #endregion

                #region Default
                default:
                    Session.SendWhisper("Ese servicio no existe. ((Usa :servicio [medico/mecanico/grua/armero]))", 1);
                    break;
                #endregion
            }

            #endregion
        }
    }
}