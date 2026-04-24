using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class UnIdleCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_unidle"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Forzar a un usuario inactivo a no estar inactivo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Vaya, te olvidas de elegir un usuario de destino!");
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
            if (User == null)
                return;

            if (User.IsAsleep)
            {
                User.UnIdle(true);
                Session.Shout("*Utiliza sus poderes divinos y fuerzas para despetar a " + User.GetUsername() + "*", 23);
                User.GetClient().SendWhisper("Te has visto obligado a despertar por " + Session.GetHabbo().Username + "!", 1);
                return;
            }
            else
            {
                Session.SendWhisper("Este usuario no está inactivo!", 1);
                return;
            }
        }
    }
}
