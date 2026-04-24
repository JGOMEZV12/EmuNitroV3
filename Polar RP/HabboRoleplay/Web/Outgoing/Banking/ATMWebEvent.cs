using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.Net;

using Polar.HabboHotel.GameClients;
using System.IO;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// ATMWebEvent class.
    /// </summary>
    class ATMWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, no se puede explotar el sistema, ir a un cajero automático");
                return;
            }

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {

                #region Open
                case "open":
                    {
                        string SendData = "";
                        SendData += Client.GetRoleplay().BankAccount + ",";
                        SendData += Client.GetRoleplay().BankChequings + ",";
                        SendData += Client.GetRoleplay().BankSavings + ",";
                        Socket.SendWS( "compose_atm|open|" + SendData);
                    }
                    break;
                #endregion

                #region Close
                case "close":
                    {
                        Client.GetRoleplay().UsingAtm = false;
                        break;
                    }
                #endregion

                #region Withdraw
                case "withdraw":
                    {
                        string[] ReceivedData = Data.Split(',');

                        int Amount;

                        if (!int.TryParse(ReceivedData[1], out Amount))
                        {
                            Socket.SendWS( "compose_atm|error|solo numeros");
                            return;
                        }

                        int WithdrawAmount = Convert.ToInt32(ReceivedData[1]);
                        string AccountType = Convert.ToString(ReceivedData[2]);

                        int ActualAmount = ((AccountType == "Checkings" ? Client.GetRoleplay().BankChequings : Client.GetRoleplay().BankSavings));

                        if (Client.GetRoleplay().TryGetCooldown("withdraw"))
                            return;

                        if (WithdrawAmount <= 0)
                        {
                            Socket.SendWS( "compose_atm|error|Invalid amount!");
                            return;
                        }

                        if (WithdrawAmount > ActualAmount || ActualAmount - WithdrawAmount <= -10000)
                        {
                            Socket.SendWS( "compose_atm|error|Usted no tiene ese tipo de dinero para retirar");
                            return;
                        }

                        int TaxAmount = Convert.ToInt32((double)WithdrawAmount * 0.05);

                        if (AccountType == "Checkings")
                        {
                            if (Client.GetRoleplay().BankAccount < 1)
                            {
                                Socket.SendWS( "compose_atm|error|¡No tienes una cuenta corriente!");
                                return;
                            }

                            if (Client.GetRoleplay().BankTarget < 1)
                            {
                                Socket.SendWS( "compose_atm|error|¡Usted no tiene tarjeta de debito, vaya al banco y pida la suya escribiendo: tarjeta!");
                                return;
                            }

                            RoleplayManager.Shout(Client, "*Saca $" + String.Format("{0:N0}", WithdrawAmount) + " De su cuenta Corriente [-100$ Por retiro]*", 5);
                            Client.SendWhisper("Si no deseas pagar comisión por retiro, dirigete al banco SalaID: 13 y escriba :retirar cantidad", 1);
                            Client.GetRoleplay().BankChequings -= WithdrawAmount;

                            Client.GetHabbo().Credits += WithdrawAmount;
                            Client.GetHabbo().Credits -= 100;
                            Client.GetRoomUser().ApplyEffect(603);
                            Client.GetHabbo().UpdateCreditsBalance();
                            Client.GetRoleplay().CooldownManager.CreateCooldown("withdraw", 1000, 5);

                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                        }
                        else
                        {
                            if (Client.GetRoleplay().BankAccount < 2)
                            {
                                Socket.SendWS( "compose_atm|error|¡No tienes una cuenta de ahorros!");
                                return;
                            }

                            if (Client.GetRoleplay().ATMAmount.IndexOf(WithdrawAmount) != -1)
                            {
                                Socket.SendWS( "compose_atm|error|Ya retiraste $" + WithdrawAmount + "!");
                                return;
                            }

                            if (Client.GetRoleplay().BankTarget < 1)
                            {
                                Socket.SendWS( "compose_atm|error|¡Usted no tiene tarjeta de debito, vaya al banco y pida la suya!");
                                return;
                            }

                            RoleplayManager.Shout(Client, "*Saca $" + String.Format("{0:N0}", WithdrawAmount) + " De su Cuenta de Ahorros y lo coloca en sus bolsillos*", 5);
                            Client.SendWhisper("Usted pagó un impuesto de $" + String.Format("{0:N0}", TaxAmount) + " Para retirar $" + String.Format("{0:N0}", WithdrawAmount) + "!", 1);

                            Client.GetHabbo().Credits += (WithdrawAmount - TaxAmount);
                            Client.GetHabbo().UpdateCreditsBalance();
                            Client.GetRoomUser().ApplyEffect(603);

                            Client.GetRoleplay().BankSavings -= WithdrawAmount;

                            Client.GetRoleplay().ATMAmount.Add(WithdrawAmount);
                            Client.GetRoleplay().CooldownManager.CreateCooldown("withdraw", 1000, 5);

                            Socket.SendWS( "compose_atm|change_balance_2|" + Client.GetRoleplay().BankSavings);
                        }
                    }
                    break;
                #endregion

                #region Deposit
                case "deposit":
                    {
                        string[] ReceivedData = Data.Split(',');

                        int Amount;

                        if (!int.TryParse(ReceivedData[1], out Amount))
                        {
                            Socket.SendWS( "compose_atm|error|Solo numeros");
                            return;
                        }

                        int DepositAmount = Convert.ToInt32(ReceivedData[1]);
                        string AccountType = Convert.ToString(ReceivedData[2]);

                        int ActualAmount = Client.GetHabbo().Credits;

                        if (Client.GetRoleplay().TryGetCooldown("deposit"))
                            return;

                        if (DepositAmount <= 0)
                        {
                            Socket.SendWS( "compose_atm|error|Monto invalido");
                            return;
                        }

                        if (DepositAmount > ActualAmount || ActualAmount - DepositAmount <= -10000)
                        {
                            Socket.SendWS( "compose_atm|error|El monto minimo para depositar es 10.000!");
                            return;
                        }

                        if (AccountType == "Checkings")
                        {
                            if (Client.GetRoleplay().BankAccount < 1)
                            {
                                Socket.SendWS( "compose_atm|error|¡No tienes una cuenta de corriente!");
                                return;
                            }

                            RoleplayManager.Shout(Client, "*Mete $" + String.Format("{0:N0}", DepositAmount) + " que sacó de su bolsillo y los deposita en su cuenta de corriente [-150$ Comisión cajero]*", 5);

                            Client.GetHabbo().Credits -= DepositAmount;
                            Client.SendWhisper("Si no deseas pagar comisión por retiro, dirigete al banco SalaID: 13 y escriba :retirar cantidad", 1);
                            Client.GetHabbo().Credits -= 150;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Client.GetRoleplay().BankChequings += DepositAmount;
                            //RoleplayManager.GiveMoneyFromCompanyNoRight(9, DepositAmount, Client, true);
                            RoleplayManager.GiveMoneyToCompany(9, Client, "bank", true, 100);

                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                        }
                        else
                        {

                            if (Client.GetRoleplay().BankAccount < 2)
                            {
                                Socket.SendWS( "compose_atm|error|¡No tienes una cuenta de ahorros!");
                                return;
                            }

                            RoleplayManager.Shout(Client, "*Mete en la ranura del cajero $" + String.Format("{0:N0}", DepositAmount) + " que saca de su bolsillo y lo deposita en su cuenta de ahorros*", 5);

                            Client.GetHabbo().Credits -= DepositAmount;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Client.GetRoleplay().BankSavings += DepositAmount;
                            //RoleplayManager.GiveMoneyFromCompanyNoRight(9, DepositAmount, Client, true);
                            RoleplayManager.GiveMoneyToCompany(9, Client, "bank", true, 100);

                            Socket.SendWS( "compose_atm|change_balance_2|" + Client.GetRoleplay().BankSavings);
                        }

                        Client.GetRoleplay().CooldownManager.CreateCooldown("deposit", 1000, 5);
                    }
                    break;
                    #endregion
            }
        }

        // ── Helper: envía texto como frame WebSocket usando ConnectionInformation
        private static void SendWS(ConnectionInformation socket, string message)
        {
            if (socket == null || string.IsNullOrEmpty(message)) return;
            socket.SendData(System.Text.Encoding.UTF8.GetBytes(message));
        }

    }
}
