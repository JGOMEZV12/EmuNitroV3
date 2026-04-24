using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Vehicles;
using System.Data;
using System.Text.RegularExpressions;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class ExitCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_exit"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Salir de una Casa."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            #region Basic Conditions
            if (Session.GetRoleplay().DrivingCar)
            {
                Session.SendWhisper("No puedes hacer eso mientras conduces.", 1);
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
            #endregion

            if (Session.GetRoleplay().TryGetCooldown("exit", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }
            #endregion

            #region Execute
            var HouseInside = PolarEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Room.Id);

            if (HouseInside != null)//Estamos dentro de una casa
            {

                RoleplayManager.Shout(Session, "*Ha Salido de la Casa*", 5);
                // Enviar a la Sala Exterior y Posición de la Puerta
                Session.GetRoleplay().ExitingHouse = true;
                Session.GetRoleplay().HouseX = HouseInside.DoorX;
                Session.GetRoleplay().HouseY = HouseInside.DoorY;
                Session.GetRoleplay().HouseZ = HouseInside.DoorZ;
                RoleplayManager.SendUserOld(Session, HouseInside.RoomId, "");
                
            }
            else
            {
                var ApartInside = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Room.Id);

                if(ApartInside == null)
                {
                    Session.SendWhisper("No te encuentras dentro de ninguna casa o apartamento.", 1);
                    return;
                }
                
                RoleplayManager.Shout(Session, "*Ha Salido del apartamento*", 5);
                RoleplayManager.SendUserOld2(Session, ApartInside.LobbyId, "");
            }

            Session.GetRoleplay().CooldownManager.CreateCooldown("exit", 1000, 5);
            #endregion
        }
    }
}