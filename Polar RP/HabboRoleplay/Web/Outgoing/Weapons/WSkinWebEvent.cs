using ConnectionManager;
using System;
using System.Linq;
using System.Text;
using Polar.Net;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Incoming.Groups;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.Messenger;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Cache;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Database.Interfaces;
using System.Text.RegularExpressions;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboRoleplay.Vehicles;
using System.Data;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Skins;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class WSkinWebEvent : IWebEvent
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

                        if (Client.GetRoleplay().TryGetCooldown("openshopwskin"))
                            return;
                        #endregion

                        Client.GetRoleplay().ViewWeaponsList = true;

                        // Obtener todos los skins ordenados por costo
                        var allSkins = WSkinManager.WSkins.Values.OrderBy(x => x.Cost).ToList();

                        // Agrupar por Name2 (nombre del arma base)
                        var skinsPorArma = allSkins.GroupBy(s => s.Name2).ToList();

                        StringBuilder html = new StringBuilder();

                        foreach (var grupo in skinsPorArma)
                        {
                            string armaBase = grupo.Key; // ej: "ak47"
                            var skins = grupo.ToList();

                            // Usar el primer skin del grupo para la imagen inicial y datos
                            var skinDefault = skins.First();

                            // Contenedor principal para el arma y sus skins
                            html.AppendLine("<div class='ft1' data-arma-base='" + armaBase + "'>");

                            // Imagen (se actualizará vía JS)
                            html.AppendLine("    <div class='circular-box'><img class='skin-image' src='" + RoleplayManager.CdnURL2 + "/armas/" + skinDefault.Name + ".png' data-base-src='" + RoleplayManager.CdnURL2 + "/armas/' /></div>");

                            html.AppendLine("    <div class='datos2'>");
                            html.AppendLine("        <span class='skin-publicname'>" + skinDefault.PublicName + "</span>");
                            html.AppendLine("        <div class='hr2'></div>");
                            html.AppendLine("        <span>Arma: " + armaBase + "</span>");
                            html.AppendLine("        <div class='hr2'></div>");
                            html.AppendLine("        <span>Skin:</span>");
                            // Selector de skins
                            html.AppendLine("        <select class='skin-selector' style='padding: 2px;width: 90%;border-width: 1px;border-color: rgba(0, 0, 0, .3);border-radius: .25rem;'>");
                            foreach (var skin in skins)
                            {
                                string selected = (skin == skinDefault) ? "selected" : "";
                                html.AppendLine($"            <option value='{skin.Name}' data-public='{skin.PublicName}' data-cost='{skin.Cost}' {selected}>{skin.PublicName}</option>");
                            }
                            html.AppendLine("        </select>");
                            html.AppendLine("        <div class='hr2'></div>");

                            // Precio y botón de compra
                            html.AppendLine("        <span style='color:#339900'><font color='orange'>" + String.Format("{0:N0}", skinDefault.Cost) + " RB</font></span><br>");
                            html.AppendLine($"        <div id='{skinDefault.Name},{skinDefault.PublicName}' class='shopskin' data-skin-name='{skinDefault.Name}' data-skin-public='{skinDefault.PublicName}' data-cost='{skinDefault.Cost}'></div>");
                            html.AppendLine("    </div>");
                            html.AppendLine("</div>");
                        }

                        string SendData = html.ToString();
                        Socket.SendWS( "compose_shop_skins|openshop|" + SendData);
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openshopwskin", 1000, 1);
                        break;
                    }
                #endregion

                #region Close Shop
                case "closeshop":
                    {
                        Client.GetRoleplay().ViewWeaponsList = false;
                        Socket.SendWS( "compose_shop_skins|closeshop|");
                        break;
                    }
                #endregion

                #region Buy Weapon
                case "shop":
                    {
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        WSkin weapon = null;
                        #region Conditions & Vars
                        if (Client.GetRoleplay().TryGetCooldown("buy"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetEffect;
                        string GetCarModel = ReceivedData[1];
                        weapon = WSkinManager.getWeapon(GetCarModel);

                        if(weapon == null)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|Ha ocurrido un problema al obtener la Información del Arma. [2]");
                            return;
                        }

                        if (Client.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name2.ToLower()) == false)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|¡No posees está arma! Comprala.");
                            return;
                        }


                        if (weapon.Cost > 100)
                        {
                            if (Client.GetRoleplay().BankChequings < weapon.Cost)
                            {
                                Socket.SendWS( "compose_shop_skins|shopmsg|No tienes dinero suficiente para comprar esa arma.");
                                return;
                            }
                        }
                        else
                        {
                            if (Client.GetHabbo().Diamonds < weapon.Cost)
                            {
                                Socket.SendWS( "compose_shop_skins|shopmsg|No tienes los Rubies suficientes para comprar esa arma.");
                                return;
                            }
                        }

                        if (weapon.Stock < 1)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|Lo sentimos, pero esta arma se ha agotado");
                            return;
                        }
                        if (Client.GetRoleplay().BankTarget < 1)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|Lo sentimos, pero aquí solo aceptamos débito y usted no tiene tarjeta, vaya al banco y solicite");
                            return;
                        }
                        if (Client.GetRoleplay().BankChequings < weapon.Cost)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|¡Lo siento, no puedes pagar una " + weapon.PublicName + " no tiene dinero en su cuenta bancaria!");
                            return;
                        }

                        if (Client.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name2.ToLower()) == false)
                        {
                            Socket.SendWS( "compose_shop_skins|shopmsg|¡No posees está arma! Comprala.");
                            return;
                        }
                        #endregion

                        #region Execute
                       
                        if (weapon.Cost > 100)
                        {
                            //Client.GetHabbo().Credits -= weapon.Cost;
                            Client.GetRoleplay().BankChequings -= weapon.Cost;
                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                            Client.GetHabbo().UpdateCreditsBalance();
                            RoleplayManager.Shout(Client, "*Compra el skin " + weapon.PublicName + " y paga $" + String.Format("{0:N0}", weapon.Cost) + "*", 5);
                            Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                        }
                        else
                        {
                            //Client.GetHabbo().Credits -= weapon.Cost;
                            Client.GetHabbo().Diamonds -= weapon.Cost;
                            Client.GetHabbo().UpdateDiamondsBalance();
                            RoleplayManager.Shout(Client, "*Compra el skin " + weapon.PublicName + " y paga " + String.Format("{0:N0}", weapon.Cost) + " rubies*", 5);
                        }

                        if (Client.GetRoleplay().EquippedWeapon != null)
                        {
                            
                            if (Client.GetRoomUser().CurrentEffect == Client.GetRoleplay().EquippedWeapon.EffectID)
                                Client.GetRoomUser().ApplyEffect(0);

                            Client.GetRoleplay().EquippedWeapon = null;
                        }

                        int CAmount = (weapon.Cost * 2) / 100;
                        RoleplayManager.GiveMoneyToCompany(5, Client, "ammunation", true, CAmount);
                        WSkinManager.WSkins[weapon.Name].Stock--;
                        RoleplayManager.UpdateMyWeaponStats(Client, "effectid", weapon.EffectID, weapon.Name2);
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
