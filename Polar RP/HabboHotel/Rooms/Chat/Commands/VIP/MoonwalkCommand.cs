using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.VIP
{
    class MoonwalkCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_moonwalk"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Camina como Michael Jackson"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            User.moonwalkEnabled = !User.moonwalkEnabled;

            if (User.moonwalkEnabled)
                Session.SendWhisper("Moonwalk activo", 1);
            else
                Session.SendWhisper("Moonwalk desactivado", 1);
        }
    }
}
