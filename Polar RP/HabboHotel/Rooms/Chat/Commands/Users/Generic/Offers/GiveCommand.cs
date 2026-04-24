using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class GiveCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_give"; }
        }

        public string Parameters
        {
            get { return "%user% %amount%"; }
        }

        public string Description
        {
            get { return "Le permite dar una cantidad de dinero deseada al usuario deseado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            int Amount;

            if (Params.Length != 3)
            {
                Session.SendWhisper("Por favor ingrese un nombre de usuario y la cantidad que desea dar!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al intentar encontrar a ese usuario, tal vez estén sin conexión.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (Session.GetRoleplay().Level < 3)
            {
                Session.SendWhisper("Su nivel es demasiado bajo para dar dinero gratis! ¡Usted debe estar al menos nivel 3!", 1);
                return;
            }

            if (Session.GetHabbo().Credits < 2)
            {
                Session.SendWhisper("El monto minimo para dar es de 2$", 1);
                return;
            }

            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes dar dinero a ti mismo!", 1);
                return;
            }

            /*if (TargetClient.MachineId == Session.MachineId)
            {
                Session.SendWhisper("¡No puedes dar dinero a otra de tus cuentas!", 1);
                return;
            }*/

            if (int.TryParse((Params[2]), out Amount))
            {
                if (Amount <= 1)
                {
                    Session.SendWhisper("Introduzca una cantidad de dinero válida MÍNIMO: 2$", 1);
                    return;
                }

                if (Session.GetHabbo().Credits < Amount)
                {
                    Session.SendWhisper("Tu no tienes $" + String.Format("{0:N0}", Amount) + " para dar", 1);
                    return;
                }

                if (Amount <= 0)
                {
                    Session.SendWhisper("El monto no puede contener -", 1);
                    return;
                }

                if (Session.GetRoleplay().TryGetCooldown("givecommand"))
                    return;

                Session.GetHabbo().Credits -= Amount;
                TargetClient.GetHabbo().Credits += Amount;

                Session.GetHabbo().UpdateCreditsBalance();
                TargetClient.GetHabbo().UpdateCreditsBalance();

                Session.Shout("*Le da a " + TargetClient.GetHabbo().Username + " $" + String.Format("{0:N0}", Amount) + "*", 4);
                TargetClient.SendWhisper("Tu recibes $" + String.Format("{0:N0}", Amount) + " de " + Session.GetHabbo().Username + "!", 1);
                Session.GetRoleplay().CooldownManager.CreateCooldown("givecommand", 1000, 3);
            }
            else
                Session.SendWhisper("Coloque un numero válido", 1);
        }
    }
}