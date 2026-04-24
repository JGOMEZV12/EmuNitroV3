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
    class ArrowCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_arrow"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Activa las teclas de flechas/desactiva"; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {

            if (!Session.GetRoleplay().ArrowEnabled)
            {
                Session.SendWhisper("Activado con éxito caminando con flechas");
                Session.GetRoleplay().ArrowEnabled = true;
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_arrowmovement:yes");
            }
            else
            {
                Session.SendWhisper("¡Ejecutado con éxito  ya usted no camina con flechas!");
                Session.GetRoleplay().ArrowEnabled = false;
                PolarEnvironment.GetGame().GetWebEventManager().SendDataDirect(Session, "compose_arrowmovement:no");
            }
            
        }
    }
}