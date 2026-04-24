using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class RoomUnmuteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_mute_room_undo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Desmutea la sala"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.RoomMuted == true)
            {
                Room.RoomMuted = false;
                Session.Shout("*Utiliza sus poderes divinos y desactiva la habitación*", 23);
            }
            else
            {
                Session.SendWhisper("Esta habitación no está silenciado", 1);
            }
        }
    }
}