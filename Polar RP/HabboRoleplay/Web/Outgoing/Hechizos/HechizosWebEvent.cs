using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.Net;
using System.Data;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Wizards;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.Weapons;
using System.Runtime.ConstrainedExecution;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class HechizosWebEvent : IWebEvent
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
            /*
            if (!Client.GetRoleplay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);
        

            switch (Action)
            {
                #region Open Shop
                case "openshop":
                    {
                        #region Conditions & Vars
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        if (Client.GetRoleplay().TryGetCooldown("openshopwizard"))
                            return;
                        #endregion

                       /* #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte al despacho para comprar el arma.", 1);
                            return;
                        }
                        #endregion*/

                        Client.GetRoleplay().ViewWizardsList = true;

                        #region HTML
                        string html = "";
                        foreach (var wizard in HechizosManager.Hechizos.Values.OrderBy(x => x.Cost))
                        {
                            string Price = (wizard.Cost > 100) ? "$ " + String.Format("{0:N0}", wizard.Cost) : wizard.Cost + " RB";

                            html += "<div class=\"ft1\">";
                            html += "<div class=\"circular-box\"><img src=\"" + RoleplayManager.CdnURL2 + "/wizards/" + wizard.Name + ".png\" /></div>";
                            html += "<div class=\"datos2\"><span>" + wizard.PublicName + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "<span>" + wizard.Name + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "Daño: <span>" + wizard.FiringDamage + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "Rango: <span>" + wizard.FiringRange + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "Vida: <span>" + wizard.Health + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "Escudo: <span>" + wizard.Shields + "</span>";
                            html += "<div class=\"hr2\"></div>";
                            html += "<span style=\"color:#339900\"><font color=\"orange\">"+Price+"</font></span><br><div id=\"" + wizard.Name + "," + wizard.PublicName + "\" class=\"shopwizard\"></div>";
                            html += "</div>";
                            html += "</div>";
                        }
                        #endregion

                        string SendData = "";
                        SendData += html;
                        Socket.SendWS( "compose_shop_wizards|openshop|" + SendData);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openshopwizard", 1000, 1);
                        break;
                    }
                #endregion

                #region Close Shop
                case "closeshop":
                    {
                        Client.GetRoleplay().ViewWizardsList = false;
                        Socket.SendWS( "compose_shop_wizards|closeshop|");
                        break;
                    }
                #endregion

                #region Buy Weapon
                case "shop":
                    {
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        Hechizos hechizo = null;
                        #region Conditions & Vars
                        if (Client.GetRoleplay().TryGetCooldown("buy"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetEffect;
                        string GetCarModel = ReceivedData[1];
                        hechizo = HechizosManager.getWizard(GetCarModel);

                        if(hechizo == null)
                        {
                            Socket.SendWS( "compose_shop_wizards|shopmsg|Ha ocurrido un problema al obtener la Información del Arma. [2]");
                            return;
                        }
                        if (hechizo.Cost > 100)
                        {
                            if (Client.GetRoleplay().BankChequings < hechizo.Cost)
                            {
                                Socket.SendWS( "compose_shop_wizards|shopmsg|No tienes dinero suficiente para comprar esa arma.");
                                return;
                            }
                        }
                        else
                        {
                            if (Client.GetHabbo().Diamonds < hechizo.Cost)
                            {
                                Socket.SendWS( "compose_shop_wizards|shopmsg|No tienes los Rubies suficientes para comprar esa arma.");
                                return;
                            }
                        }

                        if (hechizo.Stock < 1)
                        {
                            Socket.SendWS( "compose_shop_wizards|shopmsg|Lo sentimos, pero esta arma se ha agotado");
                            return;
                        }
                        if (Client.GetRoleplay().BankTarget < 1)
                        {
                            Socket.SendWS( "compose_shop_wizards|shopmsg|Lo sentimos, pero aquí solo aceptamos débito y usted no tiene tarjeta, vaya al banco y solicite");
                            return;
                        }
                        if (Client.GetRoleplay().BankChequings < hechizo.Cost)
                        {
                            Socket.SendWS( "compose_shop_wizards|shopmsg|¡Lo siento, no puedes pagar una " + hechizo.PublicName + " no tiene dinero en su cuenta bancaria!");
                            return;
                        }
                        if (Client.GetRoleplay().OwnedWeapons.ContainsKey(hechizo.Name))
                        {
                            Socket.SendWS( "compose_shop_wizards|shopmsg|Ya posees este hechizo, escoge otra.");
                            return;
                        }
                        #endregion

                        #region Execute
                        if (hechizo.Cost > 100)
                        {
                            //Client.GetHabbo().Credits -= weapon.Cost;
                            Client.GetRoleplay().BankChequings -= hechizo.Cost;
                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                            Client.GetHabbo().UpdateCreditsBalance();
                            RoleplayManager.Shout(Client, "*Compra un " + hechizo.PublicName + " y paga $" + String.Format("{0:N0}", hechizo.Cost) + "*", 5);
                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                        }
                        else
                        {
                            //Client.GetHabbo().Credits -= weapon.Cost;
                            Client.GetHabbo().Diamonds -= hechizo.Cost;
                            Client.GetHabbo().UpdateDiamondsBalance();
                            RoleplayManager.Shout(Client, "*Compra un " + hechizo.PublicName + " y paga " + String.Format("{0:N0}", hechizo.Cost) + " rubies*", 5);
                        }

                        int CAmount = (hechizo.Cost * 2) / 100;
                        RoleplayManager.GiveMoneyToCompany(5, Client, "ammunation", true, CAmount);
                        HechizosManager.Hechizos[hechizo.Name].Stock--;
                        RoleplayManager.AddWizard(Client, hechizo);
                        Client.GetRoleplay().ClearWebSocketDialogue();
                        Client.GetRoleplay().RefreshStatDialogue();
                        Client.GetRoleplay().CooldownManager.CreateCooldown("buy", 1000, 15);

                        Client.GetRoleplay().UpdateInteractingUserDialogues();
                        Client.GetRoleplay().RefreshStatDialogue();
                        break;
                        #endregion
                    }
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
