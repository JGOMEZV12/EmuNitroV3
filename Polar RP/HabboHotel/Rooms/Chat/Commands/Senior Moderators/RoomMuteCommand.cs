using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class RoomMuteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_mute_room"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Silencir la sala"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.RoomMuted == false)
            {
                Room.RoomMuted = true;

                string Msg = CommandManager.MergeParams(Params, 1);
                Session.Shout("*Utiliza sus poderes divinos y silencia la habitación*", 23);
            }
            else
            {
                Session.SendWhisper("¡Esta habitación ya está silenciada!", 1);
            }
        }
    }
}
