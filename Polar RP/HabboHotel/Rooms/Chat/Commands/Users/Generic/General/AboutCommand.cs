using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Users;
using Polar.Communication.Packets.Outgoing.Notifications;

using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Communication.Packets.Outgoing.Quests;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.Quests;
using Polar.HabboHotel.Rooms;
using System.Threading;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;
using Polar.Communication.Packets.Outgoing.Pets;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboHotel.Users.Messenger;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Availability;
using Polar.Communication.Packets.Outgoing;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class AboutCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_about"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Información del servidor."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            TimeSpan Uptime = DateTime.Now - PolarEnvironment.ServerStarted;
            TimeSpan SUptime = TimeSpan.FromMilliseconds(Environment.TickCount);
            int OnlineUsers = PolarEnvironment.GetGame().GetClientManager().Count;
            int PlayersOnline = PolarEnvironment.GetGame().GetClientManager().GetClients.Where(x => x != null && x.GetHabbo() != null).ToList().Count;
            int RoomCount = PolarEnvironment.GetGame().GetRoomManager().Count;
            string Message = "---------------------------POLAR SERVER v" + PolarEnvironment.PrettyBuild + ":---------------------------\n" +
                 "<font color=\"#8904B1\"><b>Información Polar Server:</b></font>\n" +
                 "Polar Server para " + PolarEnvironment.GetConfig().data["hotel.name"] + ", " +
                 "el cual emula las funciones basicas de habbo para nuestros usuarios, añadiendo un poco de contenido propio roleplay.\n\n" +
                 "<font color=\"#8904B1\"><b>Desarrolladores del proyecto:</b></font>\n" +
                 "  <b> · Hefesto: </b> Traducción, fix's y programación rol.\n" +
                 "  <b> · JuanGomez (JGOMEZV): </b> Mejoras y modernización.\n\n" +
                 "<font color=\"#8904B1\"><b>Estadísticas:</b></font>\n" +
                 "  <b> · Usuarios: </b> " + OnlineUsers + "\n" +
                 "  <b> · Salas Cargadas: </b> " + RoomCount + "\n" +
                 "  <b> · Tiempo: </b> " + Uptime.Days + " día(s), " + Uptime.Hours + " horas & " + Uptime.Minutes + " minutos.\n" +
                 "  <b> · Fecha: </b> " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + "\n";
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(1053); // for server time

            Session.SendMessage(new MOTDNotificationComposer(Message));

        }
    }
}