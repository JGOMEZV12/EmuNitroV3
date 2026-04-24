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
    class RoomInfoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_room_info"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te dice la identificación de la habitación y otra información sobre la habitación."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            StringBuilder room = new StringBuilder();
            Room RoomInfo = Session.GetHabbo().CurrentRoom;

            if (RoomInfo == null)
                return;

            var RoomUsers = RoomInfo.GetRoomUserManager().GetUserList().Where(x => !x.IsBot && x.GetClient() != null && x.GetClient().GetRoleplay() != null).ToList();

            room.Append("====================\nInformación de la habitación de " + RoomInfo.RoomData.Name + " (ID: " + RoomInfo.RoomData.Id + ")\n====================\n\n");
            room.Append("RoomID: " + RoomInfo.RoomData.Id + "\n");
            room.Append("Room Name: " + RoomInfo.RoomData.Name + "\n");
            room.Append("Dueño: " + RoomInfo.RoomData.OwnerName + " (ID: " + RoomInfo.RoomData.OwnerId + ")\n");
            room.Append("Usuario dentro: " + (Session.GetHabbo().GetPermissions().HasRight("mod_tool") ? String.Format("{0:N0}", RoomUsers.Count) : String.Format("{0:N0}", RoomUsers.Where(x => !x.GetClient().GetRoleplay().Invisible).ToList().Count)) + "\n\n");
            room.Append("Usuarios en la habitación:\n");

            foreach (RoomUser user in RoomUsers)
            {
                if (user == null)
                    continue;
                if (user.GetClient() == null)
                    continue;
                if (user.GetClient().GetHabbo() == null)
                    continue;
                if (user.GetClient().GetRoleplay() == null)
                    continue;
                if (user.GetClient().GetRoleplay().Invisible && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    continue;

                room.Append(user.GetClient().GetHabbo().Username + "\n");
            }

            Session.SendMessage(new MOTDNotificationComposer(room.ToString()));
        }
    }
}