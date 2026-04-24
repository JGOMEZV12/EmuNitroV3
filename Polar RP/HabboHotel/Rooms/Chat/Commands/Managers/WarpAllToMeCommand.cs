using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class WarpAllToMeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_warp_all_to_me"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Teleporta a todos los usuarios de la habitación."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            var Point = new System.Drawing.Point(Session.GetRoomUser().X, Session.GetRoomUser().Y);

            int count = 0;
            List<RoomUser> Users = Room.GetRoomUserManager().GetRoomUsers();
            foreach (RoomUser U in Users.ToList())
            {
                if (U == null || Session.GetHabbo().Id == U.UserId)
                    continue;

                var TargetPoint = new System.Drawing.Point(U.X, U.Y);

                if (Point == TargetPoint)
                    continue;

                U.ClearMovement(true);

                if (U.TeleportEnabled)
                    U.MoveTo(Point);
                else
                {
                    U.TeleportEnabled = true;
                    U.MoveTo(Point);
                    U.TeleportEnabled = false;
                }
                count++;
            }

            if (count > 0)
                Session.Shout("*Utiliza sus poderes divinos y deforma a todos en la habitación para ellos*", 23);
            else
                Session.SendWhisper("¡No había nadie más en la habitación que te deformara!", 1);
            return;
        }
    }
}