using ConnectionManager;
using Polar.Net;
using Polar.Communication.Packets.Outgoing.Inventory.Weapons;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Permissions;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Cache;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Inventory.Bots;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers.Offers;
using Polar.HabboRoleplay.VehicleOwned;
using Polar.HabboRoleplay.Vehicles;
using Polar.HabboRoleplay.Weapons;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// WeaponsWebEvent class.
    /// </summary>
    class WeaponsWebEvent : IWebEvent
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

                        if (Client.GetRoleplay().TryGetCooldown("openshopweapon"))
                            return;
                        #endregion

                        /*#region Comodin Conditions
                        Item BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte al despacho para comprar el arma.", 1);
                            return;
                        }
                        #endregion*/

                        Client.GetRoleplay().ViewWeaponsList = true;

                        // Obtener armas según VIP
                        IEnumerable<Weapon> armasVisibles;
                        if (Client.GetHabbo().VIPRank > 0 && Client.GetHabbo().GetClubManager().HasSubscription("habbo_vip"))
                            armasVisibles = WeaponManager.Weapons.Values;
                        else
                            armasVisibles = WeaponManager.Weapons.Values.Where(w => !w.isVip);

                        // Agrupar por categoría
                        var armasPorCategoria = armasVisibles
                            .OrderBy(w => w.Cost)
                            .GroupBy(w => w.Category)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        // Construcción del HTML
                        StringBuilder html = new StringBuilder();

                        // Estructura de pestañas (exactamente como la proporcionada)
                        html.AppendLine("<div class=\"tabbed-1H9Df_0 vertical-H0Fhb_0\" style=\"max-height: 490px;\">");
                        html.AppendLine("    <div id=\"Tool_Tabs\" class=\"tabs-38iVv_0\">");
                        html.AppendLine("        <div class=\"tab-2ddeR_0 GA_ArmasFuego_Tab selected-3s9hj_0\" data-category=\"ArmaDeFuego\">Armas de fuego</div>");
                        html.AppendLine("        <div class=\"tab-2ddeR_0 GA_ArmasBlancas_Tab\" data-category=\"ArmaBlanca\">Armas blancas</div>");
                        html.AppendLine("        <div class=\"tab-2ddeR_0 GA_Utiliarios_Tab\" data-category=\"Utiliario\">Utiliarios</div>");
                        html.AppendLine("    </div>");
                       

                        // Contenedores de contenido (usamos clases tab-content y IDs)
                        var categorias = new Dictionary<WeaponCategory, string>
    {
        { WeaponCategory.ArmaDeFuego, "armas_fuego" },
        { WeaponCategory.ArmaBlanca, "armas_blancas" },
        { WeaponCategory.Utiliario, "utiliarios" }
    };

                        foreach (var kvp in categorias)
                        {
                            WeaponCategory cat = kvp.Key;
                            string tabId = kvp.Value;

                            html.AppendLine($"<div class='body-1skWP_0' id='tab-{tabId}' style='padding:8px 0px 0 8px!important;'>");

                            // Mostrar armas de la categoría si las hay
                            if (armasPorCategoria.ContainsKey(cat) && armasPorCategoria[cat].Any())
                            {
                                foreach (var weapon in armasPorCategoria[cat])
                                {
                                    html.AppendLine("<div class='ft1' style='padding:1px!important;'>");
                                    html.AppendLine($"    <div class='circular-box'><img src='{RoleplayManager.CdnURL2}/armas/{weapon.Name}.png' /></div>");
                                    html.AppendLine("    <div class='datos2'>");
                                    html.AppendLine($"        <span>{weapon.PublicName}</span>");
                                    html.AppendLine("        <div class='hr2'></div>");
                                    html.AppendLine($"        <span>{weapon.Name}</span>");
                                    html.AppendLine("        <div class='hr2'></div>");
                                    html.AppendLine($"        Nivel: <span>{weapon.LevelRequirement}</span>");
                                    html.AppendLine("        <div class='hr2'></div>");
                                    html.AppendLine($"        Daño: <span>{weapon.MaxDamage}</span>");
                                    html.AppendLine("        <div class='hr2'></div>");
                                    if (weapon.Category == WeaponCategory.ArmaDeFuego)
                                    {
                                        html.AppendLine($"        Rango: <span>{weapon.Range}</span>");
                                        html.AppendLine("        <div class='hr2'></div>");
                                        html.AppendLine($"        Balas: <span>{weapon.ClipSize}</span>");
                                        html.AppendLine("        <div class='hr2'></div>");
                                    }
                                    html.AppendLine($"        <span style='color:#339900'><font color='orange'>${String.Format("{0:N0}", weapon.Cost)}</font></span><br>");
                                    html.AppendLine($"        <div id='{weapon.Name},{weapon.PublicName}' class='shopweapon'></div>");
                                    html.AppendLine("    </div>");
                                    html.AppendLine("</div>");
                                }
                            }

                            // Si es la categoría Utiliario, agregar los consumibles (balas y chaleco)
                            if (cat == WeaponCategory.Utiliario)
                            {
                                // Balas
                                int bulletCost = Convert.ToInt32(Math.Floor((double)100 / 2));
                                html.AppendLine("<div class='ft1' style='padding:1px!important;'>");
                                html.AppendLine($"    <div class='circular-box'><img src='{RoleplayManager.CdnURL2}/images/consumibles/bullets.png' /></div>");
                                html.AppendLine("    <div class='datos2'>");
                                html.AppendLine("        <span>Balas</span>");
                                html.AppendLine("        <div class='hr2'></div>");
                                html.AppendLine("        Cantidad: <span>100</span>");
                                html.AppendLine("        <div class='hr2'></div>");
                                html.AppendLine($"        <span style='color:#339900'><font color='orange'>${bulletCost}</font></span><br>");
                                html.AppendLine("        <div id='bullets,balas' class='shopweapon'></div>");
                                html.AppendLine("    </div>");
                                html.AppendLine("</div>");

                                // Chaleco
                                int kevlarCost = Convert.ToInt32(Math.Floor((double)1 / 2));
                                html.AppendLine("<div class='ft1' style='padding:1px!important;'>");
                                html.AppendLine($"    <div class='circular-box'><img src='{RoleplayManager.CdnURL2}/images/consumibles/kevlar.png' /></div>");
                                html.AppendLine("    <div class='datos2'>");
                                html.AppendLine("        <span>Chaleco</span>");
                                html.AppendLine("        <div class='hr2'></div>");
                                html.AppendLine("        Cantidad: <span>1</span>");
                                html.AppendLine("        <div class='hr2'></div>");
                                html.AppendLine($"        <span style='color:#339900'><font color='orange'>${kevlarCost}</font></span><br>");
                                html.AppendLine("        <div id='kevlar,chaleco' class='shopweapon'></div>");
                                html.AppendLine("    </div>");
                                html.AppendLine("</div>");
                            }

                            // Si no hay nada en esta pestaña (ni armas ni consumibles), mostrar mensaje
                            if ((!armasPorCategoria.ContainsKey(cat) || !armasPorCategoria[cat].Any()) && cat != WeaponCategory.Utiliario)
                            {
                                html.AppendLine("<p style='text-align:center; color:#999;'>No hay armas en esta categoría</p>");
                            }

                            html.AppendLine("</div>");
                        }

                        // Cerrar el div principal de las pestañas (el que se abrió al inicio)
                        html.AppendLine("</div>"); // Cierra el div con clases "tabbed-1H9Df_0 vertical-H0Fhb_0"

                        // Enviar al cliente
                        Socket.SendWS( "compose_shop_weapons|openshop|" + html.ToString());
                        Client.GetRoleplay().CooldownManager.CreateCooldown("openshopweapon", 1000, 1);
                        break;
                    }
                #endregion

                #region Close Shop
                case "closeshop":
                    {
                        Client.GetRoleplay().ViewWeaponsList = false;
                        Socket.SendWS( "compose_shop_weapons|closeshop|");
                        break;
                    }
                #endregion

                #region Buy Weapon
                case "shop":
                    {
                        if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room Room))
                            return;

                        Weapon weapon = null;
                        #region Conditions & Vars
                        if (Client.GetRoleplay().TryGetCooldown("buy"))
                            return;

                        string[] ReceivedData = Data.Split(',');
                        int GetEffect;
                        string GetCarModel = ReceivedData[1];
                        switch (GetCarModel)
                        {
                            case "kevlar":
                                int Cantidad = 1;


                                // Rest of your conditions...
                                if (Client.GetHabbo().CurrentRoomId != 6)
                                {
                                    Client.SendWhisper("¡El chaleco solo se compra en la tienda de armas Dirección: [BARRIO] Av. Smelly [22]!", 1);
                                    return;
                                }

                                if (Cantidad < 1)
                                {
                                    Client.SendWhisper("Debes comprar al menos 1 chaleco.");
                                    return;
                                }

                                if (Cantidad > 10)
                                {
                                    Client.SendWhisper("No puedes comprar más de 10 chalecos.");
                                    return;
                                }

                                if (Client.GetRoleplay().Armor == 60 || Client.GetRoleplay().Armor > 1)
                                {
                                    Client.SendWhisper("Actualmente tienes " + Client.GetRoleplay().Armor + " chaleco(s). Usalos para poder comprar otros.", 1);
                                    return;
                                }

                                if (Client.GetRoleplay().BankChequings < (2000 * Cantidad))
                                {
                                    Client.SendWhisper($"Necesitas tener {2000 * Cantidad}$ en tu cuenta bancaria para poder comprar {Cantidad} chaleco(s).", 1);
                                    return;
                                }

                                int precio = 2000 * Cantidad;
                                Client.Shout($"*Compra {Cantidad} chaleco(s) Kevlar por y paga con su tarjeta de débito[-{precio}$]*", 4);
                                Client.GetRoleplay().BankChequings -= precio;
                                Client.GetRoomUser().ApplyEffect(603);
                                Client.GetRoleplay().Armor = Cantidad;
                                Client.SendMessage(new WeaponsComposer(Client));
                                Client.GetRoleplay().UpdateInteractingUserDialogues();
                                Client.GetRoleplay().RefreshStatDialogue();
                                Client.GetHabbo().UpdateCreditsBalance();
                                break;
                            case "bullets":
                                int Amount = 100;

                                if (Client.GetRoleplay().EquippedWeapon == null)
                                {
                                    string WhisperMessage = "¡Tienes que tener equipada un arma para comprar sus balas!";
                                    Client.SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    return;
                                }
                                if (Client.GetRoleplay().EquippedWeapon.Name == "martillo" || Client.GetRoleplay().EquippedWeapon.Name == "cuchillo" || Client.GetRoleplay().EquippedWeapon.Name == "poder" ||
                                    Client.GetRoleplay().EquippedWeapon.Name == "bate" || Client.GetRoleplay().EquippedWeapon.Name == "sukhoi")
                                {
                                    string WhisperMessage = "¡No es un arma que posea balas!";
                                    Client.SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    return;
                                }
                                
                                if (Amount < 100)
                                {
                                    string WhisperMessage = "¡Necesitas comprar al menos 100 balas a la vez!";
                                    Client.SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }

                                if (Client.GetRoleplay().BankTarget < 1)
                                {
                                    string WhisperMessage = "¡Hey usted necesita tener una tarjeta de débito, por seguridad no manejamos dinero en efectivo!";
                                    Client.SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                    break;
                                }
                                else
                                {
                                    int Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));
                                    bool HasOffer = false;

                                    if (Client.GetRoleplay().BankChequings >= Cost)
                                    {
                                        Client.SendMessage(new ChatComposer(Client.GetRoomUser().VirtualId, "Gracias por comprar " + String.Format("{0:N0}", Amount) + " balas!", 0, 2));
                                        Client.GetRoleplay().BankChequings -= Convert.ToInt32(Math.Floor((double)Amount / 2));
                                        Client.GetHabbo().UpdateCreditsBalance();
                                        Client.GetRoleplay().Bullets += Amount;
                                        RoleplayManager.UpdateMyWeaponStats(Client, "bullets", Client.GetRoleplay().Bullets, Client.GetRoleplay().EquippedWeapon.Name);
                                        Client.SendMessage(new WeaponsComposer(Client));
                                    }
                                    else
                                    {
                                        string WhisperMessage = "Lo siento, pero no puede comprar balas su cuenta bancaria no tiene fondos";
                                        Client.SendMessage(new WhisperComposer(Client.GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                                        break;
                                    }
                                }
                                break;
                            default:
                                weapon = WeaponManager.getWeapon(GetCarModel);

                                if (weapon == null)
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|Ha ocurrido un problema al obtener la Información del Arma. [2]");
                                    return;
                                }

                                if (Client.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name.ToLower()))
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|¡Ya tienes está arma!.");
                                    return;
                                }

                                if (weapon.Cost > 100)
                                {
                                    if (Client.GetRoleplay().BankChequings < weapon.Cost)
                                    {
                                        Socket.SendWS( "compose_shop_weapons|shopmsg|No tienes dinero suficiente para comprar esa arma.");
                                        return;
                                    }
                                }
                                else
                                {
                                    if (Client.GetHabbo().Diamonds < weapon.Cost)
                                    {
                                        Socket.SendWS( "compose_shop_weapons|shopmsg|No tienes los Rubies suficientes para comprar esa arma.");
                                        return;
                                    }
                                }

                                if (weapon.Stock < 1)
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|Lo sentimos, pero esta arma se ha agotado");
                                    return;
                                }
                                if (Client.GetRoleplay().BankTarget < 1)
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|Lo sentimos, pero aquí solo aceptamos débito y usted no tiene tarjeta, vaya al banco y solicite");
                                    return;
                                }
                                if (Client.GetRoleplay().BankChequings < weapon.Cost)
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|¡Lo siento, no puedes pagar una " + weapon.PublicName + " no tiene dinero en su cuenta bancaria!");
                                    return;
                                }
                                if (Client.GetRoleplay().OwnedWeapons.ContainsKey(weapon.Name))
                                {
                                    Socket.SendWS( "compose_shop_weapons|shopmsg|Ya posees está arma, escoge otra.");
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
                                    RoleplayManager.Shout(Client, "*Compra un " + weapon.PublicName + " y paga $" + String.Format("{0:N0}", weapon.Cost) + "*", 5);
                                    Socket.SendWS( "compose_atm|change_balance_1|" + Client.GetRoleplay().BankChequings);
                                }
                                else
                                {
                                    //Client.GetHabbo().Credits -= weapon.Cost;
                                    Client.GetHabbo().Diamonds -= weapon.Cost;
                                    Client.GetHabbo().UpdateDiamondsBalance();
                                    RoleplayManager.Shout(Client, "*Compra un " + weapon.PublicName + " y paga " + String.Format("{0:N0}", weapon.Cost) + " rubies*", 5);
                                }

                                int CAmount = (weapon.Cost * 2) / 100;
                                RoleplayManager.GiveMoneyToCompany(5, Client, "ammunation", true, CAmount);
                                WeaponManager.Weapons[weapon.Name].Stock--;
                                RoleplayManager.AddWeapon(Client, weapon);
                                Client.SendMessage(new WeaponsComposer(Client));
                                Client.GetRoleplay().ClearWebSocketDialogue();
                                Client.GetRoleplay().RefreshStatDialogue();
                                Client.GetRoleplay().CooldownManager.CreateCooldown("buy", 1000, 15);

                                Client.GetRoleplay().UpdateInteractingUserDialogues();
                                Client.GetRoleplay().RefreshStatDialogue();
                                break;
                        }
                      
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
