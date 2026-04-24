using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms.AI;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Pets;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Misc;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class ActiveBotsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_active_bots"; }
        }

        public string Parameters
        {
            get { return "%command%"; }
        }

        public string Description
        {
            get { return "Testing bots"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            ConcurrentBag<RoomUser> ActiveBots = new ConcurrentBag<RoomUser>();

            foreach (Room LoadedRoom in PolarEnvironment.GetGame().GetRoomManager().GetRooms().ToList())
            {
                if (LoadedRoom == null)
                    continue;

                foreach (RoomUser Bot in LoadedRoom.GetRoomUserManager().GetBotList().ToList())
                {
                    ActiveBots.Add(Bot);
                }

            }

            string String = null;
            String += "Active bots:\n------------------\n\n";
            foreach (RoomUser Bot in ActiveBots)
            {
                if (Bot == null)
                    continue;

                if (Bot.GetBotRoleplay() == null)
                    continue;

                if (Bot.GetBotRoleplayAI() == null)
                    continue;

                Room BotRoom = Bot.GetBotRoleplayAI().GetRoom();

                if (BotRoom == null)
                    continue;

                String += "Bot name: " + Bot.GetBotRoleplay().Name + "\n";
                String += "Room name: " + BotRoom.Name + "\n";
                String += "Room ID: " + BotRoom.Id + "\n";
                String += "Current users: " + BotRoom.UsersNow + "\n\n";
            }

            Session.SendMessage(new MOTDNotificationComposer(String));
        }
    }
}
