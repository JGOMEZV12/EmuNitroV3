using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class HotRoomsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_hot_rooms"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te dice todas las habitaciones ocupadas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder Rooms = new StringBuilder();

            foreach (Room room in PolarEnvironment.GetGame().GetRoomManager().GetRooms().ToList().OrderByDescending(key => key.UserCount))
            {
                if (room.UserCount <= 0)
                    continue;

                Rooms.Append("[" + room.RoomId + "] - " + room.RoomData.Name + " - Users: " + room.UserCount + "\n");
            }

            Session.SendMessage(new MOTDNotificationComposer(Rooms.ToString()));
        }
    }
}