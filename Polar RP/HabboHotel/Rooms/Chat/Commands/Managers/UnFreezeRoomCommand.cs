using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class UnFreezeRoomCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_freeze_room_undo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "¡Descongela a todos los usuarios congelados en la sala!"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.GetRoomUserManager().GetUserList().ToList().Count <= 1)
            {
                Session.SendWhisper("Usted es la única persona en la sala!", 1);
                return;
            }

            int count = 0;
            foreach (RoomUser User in Room.GetRoomUserManager().GetUserList().ToList())
            {
                if (User == null || User.GetClient() == null || User.GetClient().GetHabbo() == null)
                    continue;

                if (!User.Frozen)
                    continue;

                count++;
                User.Frozen = false;

                if (User.CurrentEffect == 12)
                    User.ApplyEffect(0);

                User.GetClient().SendWhisper("Has sido descongelado por " + Session.GetHabbo().Username + "!", 1);
            }

            if (count > 0)
            {
                Session.Shout("*Utiliza sus poderes divinos y descongela toda la habitación*", 23);
                return;
            }
            else
            {
                Session.SendWhisper("No hay usuarios congelados en la habitación!", 1);
                return;
            }
        }
    }
}
