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
    class ChangeLogCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_change_log"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Actualizaciones recientes en el holo."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {

            if (string.IsNullOrWhiteSpace(PolarEnvironment.GetDBConfig().DBData["welcome_message"]))
            {
                Session.SendWhisper("Oops, there is no change log!", 1);
                return;
            }
            
            Session.SendMessage(new MOTDNotificationComposer(PolarEnvironment.GetDBConfig().DBData["welcome_message"].Replace("\\r\\n", "\n")));
        }
    }
}