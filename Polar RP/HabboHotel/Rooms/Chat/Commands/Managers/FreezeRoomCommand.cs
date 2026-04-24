using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class FreezeRoomCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_freeze_room"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Congela a todos los usuarios en la sala actual!"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Room.GetRoomUserManager().GetUserList().ToList().Count <= 1)
            {
                Session.SendWhisper("Usted es la única persona en la sala!", 1);
                return;
            }

            foreach (RoomUser User in Room.GetRoomUserManager().GetUserList().ToList())
            {
                if (User == null || User.GetClient() == null || User.GetClient().GetHabbo() == null)
                    continue;

                if (User.GetClient() == Session)
                    continue;

                if (User.Frozen)
                    continue;

                if (User.GetClient().GetRoleplay().EquippedWeapon != null)
                    User.GetClient().GetRoleplay().EquippedWeapon = null;

                User.Frozen = true;
                User.ClearMovement(true);

                if (User.CurrentEffect != 12)
                    User.ApplyEffect(EffectsList.Ice);

                User.GetClient().SendWhisper("Has sido congelado por " + Session.GetHabbo().Username + "!", 1);
            }

            Session.Shout("*Utiliza sus poderes divinos y congela toda la habitación*", 23);
            return;
        }
    }
}
