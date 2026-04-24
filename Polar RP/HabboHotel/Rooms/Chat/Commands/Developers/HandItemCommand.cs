using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class HandItemCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_handitem"; }
        }

        public string Parameters
        {
            get { return "%item%"; }
        }

        public string Description
        {
            get { return "Ten algo en la mano."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            int ItemId = 0;
            if (!int.TryParse(Convert.ToString(Params[1]), out ItemId))
            {
                Session.SendWhisper("coloque un item válido.", 1);
                return;
            }

            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            User.CarryItem(ItemId);
        }
    }
}
