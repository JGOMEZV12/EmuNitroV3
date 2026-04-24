using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class RoomBadgeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_badge_room"; }
        }

        public string Parameters
        {
            get { return "%badge%"; }
        }

        public string Description
        {
            get { return "Give a badge to the entire room!"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Please enter the name of the badge you'd like to give to the room.", 1);
                return;
            }

            Badges.BadgeDefinition BadgeDefinition = null;
            if (!PolarEnvironment.GetGame().GetBadgeManager().TryGetBadge(Params[1].ToUpper(), out BadgeDefinition))
            {
                Session.SendWhisper("The badge definitions do not contain this badge!", 1);
                return;
            }

            foreach (RoomUser User in Room.GetRoomUserManager().GetUserList().ToList())
            {
                if (User == null || User.GetClient() == null || User.GetClient().GetHabbo() == null)
                    continue;

                if (!User.GetClient().GetHabbo().GetBadgeComponent().HasBadge(Params[1]))
                {
                    User.GetClient().GetHabbo().GetBadgeComponent().GiveBadge(Params[1], true, User.GetClient());
                    User.GetClient().SendNotification("You have just been given a badge!");
                }
                else
                    User.GetClient().SendWhisper(Session.GetHabbo().Username + " tried to give you a badge, but you already have it!", 1);
            }

            Session.SendWhisper("You have successfully given every user in this room the " + Params[1] + " badge!", 1);
        }
    }
}
