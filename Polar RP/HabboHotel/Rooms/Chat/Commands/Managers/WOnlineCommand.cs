using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Fleck;
using Polar.Communication.Packets.Outgoing.Notifications;
using Newtonsoft.Json;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class WOnlineCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_wonline"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Usuarios en línea con websocke"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder SocketAppend = new StringBuilder();
            int UserCount = 0;

            if (PolarEnvironment.GetGame().GetWebEventManager() == null)
            {
                Session.SendWhisper("Websocket es nulo por alguna razón?", 1);
                return;
            }

            lock (PolarEnvironment.GetGame().GetWebEventManager()._webSockets)
            {

                Session.SendWhisper("Total websocketusers/usuarios conectados: " +
                    PolarEnvironment.GetGame().GetWebEventManager()._webSockets.Count + "/" + PolarEnvironment.GetGame().GetClientManager().GetClients.Where(Client => Client != null && !Client.LoggingOut).ToList().Count);

                lock (PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {
                        if (Client == null)
                            continue;

                        if (Client.LoggingOut)
                            continue;

                        if (Client.GetHabbo() == null)
                            continue;

                        if (Client.GetRoleplay() == null)
                            continue;

                        if (!PolarEnvironment.GetGame().GetWebEventManager().SocketReady(Client))
                            continue;

                        SocketAppend.Append("Nombre: " + Client.GetHabbo().Username + "\n");
                        SocketAppend.Append("Socket Socket: True\n\n");
                        UserCount++;
                    }

                    foreach (GameClient Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                    {

                        if (Client == null)
                            continue;

                        if (Client.LoggingOut)
                            continue;

                        if (Client.GetHabbo() == null)
                            continue;

                        if (Client.GetRoleplay() == null)
                            continue;

                        if (Client.GetRoleplay().WebSocketConnection != null)
                            continue;

                        SocketAppend.Append("Usuario: " + Client.GetHabbo().Username + "\n");
                        SocketAppend.Append("Socket running: False\n\n");
                        UserCount++;
                    }
                }

                string SocketUsers = "===============================\n";
                SocketUsers += "Usuarios conectados: " + PolarEnvironment.GetGame().GetWebEventManager()._webSockets.Count + "/" + UserCount + "\n";
                SocketUsers += "===============================\n\n";
                SocketUsers += SocketAppend;

                Session.SendMessage(new MOTDNotificationComposer(SocketUsers));
            }

            return;
        }
    }
}
