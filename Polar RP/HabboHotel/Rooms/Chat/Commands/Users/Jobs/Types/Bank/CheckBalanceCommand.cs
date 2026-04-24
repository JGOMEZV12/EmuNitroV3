using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank
{
    class CheckBalanceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_check_balance"; }
        }

        public string Parameters
        {
            get { return "%user% %account%"; }
        }

        public string Description
        {
            get { return "Compruebe el saldo de tipo de cuenta del usuario de destino."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions

            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese un tipo de banco y destino. (chequings, savings)");
                return;
            }

            GameClient Target = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (Target == null)
            {
                Session.SendWhisper("¡Uy, no pudo encontrar ese usuario!");
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "checkbalance"))
            {
                Session.SendWhisper("Lo siento, no trabajas en la corporación del banco", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("checkbalance"))
                return;

            #endregion

            #region Execute

            if (Params.Length < 3)
            {
                if (Target.GetRoleplay().BankAccount <= 0)
                {
                    Session.SendWhisper("Este usuario no tiene una cuenta Chequings", 1);
                    return;
                }

                Session.Shout("*Revisa la cuenta de " + Target.GetHabbo().Username + "'s Saldo en su Cuenta Chequings*", 4);
                Session.SendWhisper(Target.GetHabbo().Username + " Tienen un saldo de: $" + String.Format("{0:N0}", Target.GetRoleplay().BankChequings) + " En su cuenta Chequings", 1);
                Session.GetRoleplay().CooldownManager.CreateCooldown("checkbalance", 1000, 3);
                return;
            }
            else
            {
                switch (Params[2].ToLower())
                {
                    case "chequings":
                    case "checkings":
                        {
                            if (Target.GetRoleplay().BankAccount <= 0)
                            {
                                Session.SendWhisper("Este usuario no tiene una cuenta Chequings", 1);
                                break;
                            }

                            Session.Shout("*Revisa la cuenta de: " + Target.GetHabbo().Username + "'s Saldo en su cuenta Chequings*", 4);
                            Session.SendWhisper(Target.GetHabbo().Username + " Tiene: $" + Target.GetRoleplay().BankChequings + " en su cuenta Chequings", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("checkbalance", 1000, 3);
                            break;
                        }
                    case "savings":
                        {
                            if (Target.GetRoleplay().BankAccount <= 1)
                            {
                                Session.SendWhisper("¡Este usuario no tiene una cuenta de ahorros!", 1);
                                break;
                            }

                            Session.Shout("*Revisa la cuenta de " + Target.GetHabbo().Username + "'s Saldo en su cuenta de Savings*", 4);
                            Session.SendWhisper(Target.GetHabbo().Username + " Tiene $" + Target.GetRoleplay().BankSavings + " En su cuenta de Savings!", 1);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("checkbalance", 1000, 3);
                            break;
                        }
                    default:
                        {
                            Session.SendWhisper("Ingrese un tipo de cuenta bancaria válido (chequings, savings)");
                            break;
                        }
                }
            }

            #endregion
        }
    }
}
