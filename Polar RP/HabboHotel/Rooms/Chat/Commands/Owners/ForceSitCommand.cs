using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class ForceSitCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_force_sit"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Force another user to sit."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Oops, you forgot to choose a target user!");
                return;
            }

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
            if (User == null)
                return;

            if (User.Statusses.ContainsKey("lay") || User.isLying || User.RidingHorse || User.IsWalking)
                return;

            var Items = Room.GetGameMap().GetAllRoomItemForSquare(User.X, User.Y);
            bool HasChair = Items.ToList().Where(x => x != null && x.GetBaseItem().IsSeat).Count() > 0;

            if (HasChair || User.GetClient().GetHabbo().VIPRank > 0)
                return;

            if (!User.Statusses.ContainsKey("sit"))
            {
                if ((User.RotBody % 2) == 0)
                {
                    if (User == null)
                        return;

                    try
                    {
                        User.Statusses.Add("sit", "1.0");
                        User.Z -= 0.35;
                        User.isSitting = true;
                        User.UpdateNeeded = true;
                    }
                    catch { }
                }
                else
                {
                    User.RotBody--;
                    User.Statusses.Add("sit", "1.0");
                    User.Z -= 0.35;
                    User.isSitting = true;
                    User.UpdateNeeded = true;
                }

                User.Frozen = true;
                User.ClearMovement(true);
                User.ForceSit = true;

                PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(User.GetClient(), ":unequip");
                Session.Shout("*Looks into " + User.GetClient().GetHabbo().Username + "'s eyes, instilling fear into them causing them to sit down*", 23);
            }
            else if (User.isSitting == true)
            {
                User.Z += 0.35;
                User.RemoveStatus("sit");
                User.isSitting = false;
                User.UpdateNeeded = true;

                User.Frozen = false;
                User.ClearMovement(true);
                User.ForceSit = false;


                Session.Shout("*Looks into " + User.GetClient().GetHabbo().Username + "'s eyes causing them to stand up in fear*", 23);
            }
        }
    }
}
