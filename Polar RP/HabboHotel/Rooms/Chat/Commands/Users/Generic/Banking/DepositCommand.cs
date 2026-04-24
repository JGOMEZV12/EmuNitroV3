using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Banking
{
    class DepositCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_banking_deposit"; }
        }

        public string Parameters
        {
            get { return "%amount% %account%"; }
        }

        public string Description
        {
            get { return "Deposita dinero en su cuenta deseada."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca la cantidad que desea depositar.", 1);
                return;
            }

            if (Room.BankEnabled == false)
            {
                Session.SendWhisper("¡Debe estar en el banco para depositar dinero en sus cuentas!", 1);
                return;
            }

            if (Session.GetRoleplay().BankAccount <= 0)
            {
                Session.SendWhisper("¡No tienes cuentas bancarias! Pida a un trabajador bancario que abra una cuenta para usted.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("deposit"))
                return;

            int Amount;
            if (int.TryParse(Params[1], out Amount))
            {
                if (Amount <= 0)
                {
                    Session.SendWhisper("Por favor ingrese una cantidad válida para depositar", 1);
                    return;
                }

                if (Session.GetHabbo().Credits < Amount)
                {
                    Session.SendWhisper("no tienes $" + Amount + " para depositar", 1);
                    return;
                }
            }
            else
            {
                Session.SendWhisper("Por favor ingrese un número para depositar!", 1);
                return;
            }
            #endregion

            #region Execute
            if (Params.Length == 2)
            {
                if (Session.GetRoleplay().BankAccount < 1)
                {
                    Session.SendWhisper("¡No tienes una cuenta corriente! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                    return;
                }

                Session.Shout("*mete $" + String.Format("{0:N0}", Amount) + " De su bolsillo y lo deposita en su Cuenta Corriente*", 5);

                Session.GetHabbo().Credits -= Amount;
                Session.GetHabbo().UpdateCreditsBalance();

                Session.GetRoleplay().BankChequings += Amount;
                Session.GetRoleplay().CooldownManager.CreateCooldown("deposit", 1000, 5);
            }
            else
            {
                switch (Params[2].ToLower())
                {
                    case "1":
                    case "chequings":
                    case "corriente":
                        {
                            if (Session.GetRoleplay().BankAccount < 1)
                            {
                                Session.SendWhisper("¡No tienes una cuenta de chequings! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                                return;
                            }

                            Session.Shout("*mete $" + String.Format("{0:N0}", Amount) + " De su bolsillo y lo deposita en su Cuenta corriente*", 5);

                            Session.GetHabbo().Credits -= Amount;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankChequings += Amount;
                            Session.GetRoleplay().CooldownManager.CreateCooldown("deposit", 1000, 5);
                            break;
                        }
                    case "2":
                    case "savings":
                    case "ahorros":
                        {
                            if (Session.GetRoleplay().BankAccount < 2)
                            {
                                Session.SendWhisper("¡No tienes una cuenta de ahorros! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                                return;
                            }

                            Session.Shout("*mete $" + String.Format("{0:N0}", Amount) + " De su bolsillo y lo deposita en su cuenta de ahorros*", 5);

                            Session.GetHabbo().Credits -= Amount;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankSavings += Amount;
                            Session.GetRoleplay().CooldownManager.CreateCooldown("deposit", 1000, 5);
                            break;
                        }
                    default:
                        {
                            Session.SendWhisper("¡Utilice 'corriente' o 'ahorros' para su tipo de cuenta!", 1);
                            break;
                        }
                }
            }
            #endregion
        }
    }
}