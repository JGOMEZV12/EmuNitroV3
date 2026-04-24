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
    class AtransferirCommand : IChatCommand
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
            get { return "Transferiere tu cuenta dinero de tu cuenta bancario a otro usuario."; }
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
            if (TargetClient == Session)
            {
                Session.SendWhisper("¡No puedes dar dinero a ti mismo!", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("computer_flatscreen", 1))
            {
                Session.Shout("¡Rayos! Necesito una computadora para hacer transferencias bancarias", 1);
                return;
            }

           /* if (TargetClient.MachineId == Session.MachineId)
            {
                Session.SendWhisper("¡No puedes dar dinero a otra de tus cuentas!", 1);
                return;
            }*/

            if (TargetClient.GetRoleplay().BankAccount < 2)
            {
                Session.SendWhisper("¡Esta persona no tiene cuenta de ahorros!", 1);
                return;
            }

            if (int.TryParse((Params[2]), out Amount))
            {
                if (Amount <= 0)
                {
                    Session.SendWhisper("Introduzca una cantidad de dinero válida", 1);
                    return;
                }

                if (Session.GetRoleplay().BankSavings < 0)
                {
                    Session.SendWhisper("Tu no tienes $" + String.Format("{0:N0}", Amount) + " para transferir", 1);
                    return;
                }

                if (Session.GetRoleplay().BankAccount < 2)
                {
                    Session.SendWhisper("Usted no posee ninguna cuenta de ahorros, vaya al banco [Sala: 53] y abra una", 1);
                    return;
                }

                if (Session.GetRoleplay().BankSavings < 30000)
                {
                    Session.SendWhisper("El monto mínimo para transferir es de 30.000$  ", 1);
                    return;
                }

                if (Session.GetRoleplay().TryGetCooldown("givecommand"))
                    return;

                Session.GetRoleplay().BankSavings -= Amount;
                TargetClient.GetRoleplay().BankSavings += Amount;
                Session.Shout("*Transfiere de su cuenta AHORROS: $" + String.Format("{0:N0}", Amount) + " a " + TargetClient.GetHabbo().Username + " *", 4);
                Session.SendWhisper("[HETIBAK] La transferencia ha sido exitosa, gracias a nuestra tecnología " + TargetClient.GetHabbo().Username + " ya recibió su dinero");
                TargetClient.SendWhisper("¡Usted ha recibido una transferencia por $" + String.Format("{0:N0}", Amount) + " de " + Session.GetHabbo().Username + "!", 1);
                Session.GetRoleplay().CooldownManager.CreateCooldown("givecommand", 1000, 3);
            }
            else
                Session.SendWhisper("Coloque un numero válido", 1);
        }
    }
}