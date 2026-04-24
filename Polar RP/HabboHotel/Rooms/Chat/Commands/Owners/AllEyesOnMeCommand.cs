using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Pathfinding;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class AllEyesOnMeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_all_eyes_on_me"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Makes all the users in the rooms eyes turn towards you."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser ThisUser = Session.GetRoomUser();
            if (ThisUser == null)
                return;

            List<RoomUser> Users = Room.GetRoomUserManager().GetRoomUsers();
            foreach (RoomUser U in Users.ToList())
            {
                if (U == null || Session.GetHabbo().Id == U.UserId)
                    continue;

                U.SetRot(Rotation.Calculate(U.X, U.Y, ThisUser.X, ThisUser.Y), false);
            }

            Session.Shout("*Utiliza sus poderes divinos and make all the users in the room look at them*", 23);
        }
    }
}
