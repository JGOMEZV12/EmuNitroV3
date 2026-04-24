using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.GameClients;


namespace Polar.HabboHotel.Rooms.Chat.Commands.User.Fun
{
    class DiscoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_disco"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Easter Egg"; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (Room != null || !Room.CheckRights(Session))
            {
                Room.DiscoMode = !Room.DiscoMode;

                if (Room.DiscoMode)
                    Session.SendWhisper("Discomode activado.", 1);
                else
                    Session.SendWhisper("Discomode desactivado.", 1);
            }
        }
    }
}