using ConnectionManager;
﻿using System;
using System.Linq;
using System.Text;
using Polar.Net;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.IO;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Food;
using System.Data;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    class FoodWebEvent : IWebEvent
    {
        public void Execute(GameClient Client, string Data, ConnectionInformation Socket)
        {
            if (Client == null || Socket == null)
                return;

            if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) ||
                !PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            if (string.IsNullOrEmpty(Data))
                return;

            string Action = Data.Contains(',') ? Data.Split(',')[0] : Data;

            switch (Action)
            {
                // ── Abrir menú ────────────────────────────────────────────────────
                case "open":
                {
                    string[] parts = Data.Split(',');
                    if (parts.Length < 2) return;

                    var ServableFoods = FoodManager.GetServableBotItems(parts[1]);
                    if (ServableFoods == null || !ServableFoods.Any())
                    {
                        Socket.SendWS( "compose_shop_restaurant|error|No hay alimentos disponibles");
                        return;
                    }

                    var html = new StringBuilder();
                    foreach (var Food in ServableFoods.OrderBy(x => x.Cost))
                    {
                        if (Food == null) continue;

                        ItemData itemData;
                        if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Food.ItemId, out itemData) || itemData == null)
                            continue;

                        html.Append("<div data-balloon=\"" + Food.Name + "\" data-balloon-pos=\"right\" id=\"comprarcomida\" class=\"p-1 w-1/2\" style=\"box-sizing: border-box; width: 11%;\">");
                        html.Append("<div class=\"box p-4 flex items-center cursor-pointer-r hover:bg-dark-3 menu-comida-item\" style=\"padding: 0.5rem !important;\" data-comida=\"" + Food.Name + "\">");
                        html.Append("<div class=\"flex justify-center items-center\" style=\"width: 50px;height: 50px;\">");
                        html.Append("<img src=\"" + RoleplayManager.CDNSWF + "/dcr/hof_furni/" + itemData.ItemName + "_icon.png\" class=\"mr-2 flex-none\" style=\"left: 15%; position: relative;\">");
                        html.Append("</div></div></div>");
                    }

                    Socket.SendWS( "compose_shop_restaurant|open|" + html);
                    break;
                }

                // ── Comprar / pedir comida ────────────────────────────────────────
                case "shop":
                {
                    if (Client.GetRoomUser() == null) return;
                    if (!RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId, out Room room)) return;

                    string[] parts = Data.Split(',');
                    if (parts.Length < 2) return;

                    string DesiredFood = parts[1];

                    RoomUser roomUser = room.GetRoomUserManager().GetRoomUserByHabbo(Client.GetHabbo().Id);
                    if (roomUser == null) return;

                    // Buscar el bot de comida en la sala
                    var BotUser = RoleplayBotManager.GetDeployedBotById(10);
                    if (BotUser == null)
                    {
                        Socket.SendWS( "compose_shop_restaurant|error|Bot no disponible");
                        return;
                    }

                    // Si el bot ya tiene un timer de serving activo, matarlo antes de aceptar
                    // un nuevo pedido (evita timers en paralelo por clicks múltiples en la web)
                    if (BotUser.GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("serving"))
                        BotUser.GetBotRoleplay().TimerManager.ActiveTimers["serving"].EndTimer();

                    // Disparar el pedido como si el usuario lo dijera en el chat
                    roomUser.OnChat(roomUser.LastBubble, "servir " + DesiredFood, false, string.Empty);

                    if (Client.GetRoleplay() != null)
                    {
                        Client.GetRoleplay().ClearWebSocketDialogue();
                        Client.GetRoleplay().RefreshStatDialogue();
                        Client.GetRoleplay().UpdateInteractingUserDialogues();
                        Client.GetRoleplay().RefreshStatDialogue();
                    }
                    break;
                }

                // ── Abrir/cerrar panel ────────────────────────────────────────────
                case "openfood":
                case "close":
                    Socket.SendWS( "compose_shop_restaurant|" + Action + "|");
                    break;
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