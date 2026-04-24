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
    class WithdrawCommand : IChatCommand
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
            get { return "Retira dinero de su cuenta deseada."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Amount;
            int TaxAmount;
            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese la cantidad que desea retirar.", 1);
                return;
            }

            if (Room.BankEnabled == false)
            {
                Session.SendWhisper("Usted debe estar en el banco para retirar dinero a sus cuentas", 1);
                return;
            }

            if (Session.GetRoleplay().BankAccount <= 0)
            {
                Session.SendWhisper("¡No tienes cuentas bancarias! Pida a un trabajador bancario que abra una cuenta para usted.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("withdraw"))
                return;
            #endregion

            #region Execute
            if (Params.Length == 2)
            {
                if (Session.GetRoleplay().BankAccount < 1)
                {
                    Session.SendWhisper("Usted no tiene una cuenta de chequings! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                    return;
                }

                if (int.TryParse(Params[1], out Amount))
                {
                    if (Amount <= 0)
                    {
                        Session.SendWhisper("Por favor ingrese una cantidad válida para retirar", 1);
                        return;
                    }

                    if (Session.GetRoleplay().BankChequings < Amount)
                    {
                        Session.SendWhisper("No tienes $" + String.Format("{0:N0}", Amount) + " para retirar", 1);
                        return;
                    }
                }
                else
                {
                    Session.SendWhisper("Por favor ingrese un número para retirar", 1);
                    return;
                }

                Session.Shout("*Saca $" + String.Format("{0:N0}", Amount) + " De su cuenta corriente y lo coloca en sus bolsillos*", 5);

                Session.GetHabbo().Credits += Amount;
                Session.GetHabbo().UpdateCreditsBalance();

                Session.GetRoleplay().BankChequings -= Amount;
                Session.GetRoleplay().CooldownManager.CreateCooldown("withdraw", 1000, 5);
            }
            else
            {
                switch (Params[2].ToLower())
                {
                    case "1":
                    case "corriente":
                        {
                            if (Session.GetRoleplay().BankAccount < 1)
                            {
                                Session.SendWhisper("Usted no tiene una cuenta de chequings! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                                return;
                            }

                            if (int.TryParse(Params[1], out Amount))
                            {
                                if (Session.GetRoleplay().BankChequings < Amount)
                                {
                                    Session.SendWhisper("No tienes $" + String.Format("{0:N0}", Amount) + " para retirar", 1);
                                    return;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Por favor ingrese un número para retirar", 1);
                                return;
                            }

                            Session.Shout("*Saca $" + String.Format("{0:N0}", Amount) + " De su Cuenta corriente y la coloca en sus bolsillos*", 5);

                            Session.GetHabbo().Credits += Amount;
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankChequings -= Amount;
                            Session.GetRoleplay().CooldownManager.CreateCooldown("withdraw", 1000, 5);
                            break;
                        }
                    case "2":
                    case "ahorro":
                        {
                            if (Session.GetRoleplay().BankAccount < 2)
                            {
                                Session.SendWhisper("Usted no tiene una cuenta de ahorros! ¡Por favor, pídale a un trabajador bancario que abra uno para usted!", 1);
                                return;
                            }

                            if (int.TryParse(Params[1], out Amount))
                            {
                                TaxAmount = Convert.ToInt32((double)Amount * 0.05);

                                if (Amount < 20)
                                {
                                    Session.SendWhisper("El monto mínimo que puede retirar de su cuenta de ahorros es $20", 1);
                                    return;
                                }

                                if (Session.GetRoleplay().BankSavings < Amount)
                                {
                                    Session.SendWhisper("No tienes $" + String.Format("{0:N0}", Amount) + " para retirar", 1);
                                    return;
                                }

                                if (Session.GetRoleplay().ATMAmount.IndexOf(Amount) != -1)
                                {
                                    Session.SendWhisper("You have already withdrawn $" + Amount + "!", 1);
                                    return;
                                }

                                if (Params.Length < 4)
                                {
                                    Session.SendWhisper("Te costará $" + String.Format("{0:N0}", TaxAmount) + " para retirar $" + Amount + " De su cuenta de ahorros! escriba ':retirar " + String.Format("{0:N0}", Amount) + " 'ahorro yes'   Si acepta esa tasa de impuesto.", 1);
                                    return;
                                }

                                if (Params[3].ToLower() != "yes")
                                {
                                    Session.SendWhisper("Te costará $" + String.Format("{0:N0}", TaxAmount) + " para retirar $" + String.Format("{0:N0}", Amount) + " De su cuenta de ahorros! Tipo ':withdraw " + String.Format("{0:N0}", Amount) + " 'ahorro yes'   Si acepta esa tasa de impuesto.", 1);
                                    return;
                                }
                            }
                            else
                            {
                                Session.SendWhisper("Please input a number to withdraw!", 1);
                                return;
                            }

                            Session.Shout("*Saca $" + String.Format("{0:N0}", Amount) + " se su cuenta de ahorros*", 5);
                            Session.SendWhisper("Usted pagó un impuesto de $" + String.Format("{0:N0}", TaxAmount) + " Para retirar" + String.Format("{0:N0}", Amount) + "!", 1);

                            Session.GetHabbo().Credits += (Amount - TaxAmount);
                            Session.GetHabbo().UpdateCreditsBalance();

                            Session.GetRoleplay().BankSavings -= Amount;

                            Session.GetRoleplay().ATMAmount.Add(Amount);
                            Session.GetRoleplay().CooldownManager.CreateCooldown("withdraw", 1000, 5);
                            break;
                        }
                    default:
                        {
                            Session.SendWhisper("Por favor use 'corriente' o 'ahorro' Para su tipo de cuenta", 1);
                            break;
                        }
                }
            }
            #endregion
        }
    }
}