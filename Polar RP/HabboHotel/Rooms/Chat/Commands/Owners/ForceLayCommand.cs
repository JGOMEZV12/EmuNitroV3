using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class ForceLayCommand : IChatCommand
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
            get { return "Forzar a otro usuario a poner."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Oops, olvidaste colocar el nombre");
                return;
            }

            GameClient TargetSession = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);

            if (User == null)
                return;

            if (User == null)
                return;

            if (User.ForceSit)
                return;

            if (User.Statusses.ContainsKey("sit") || User.isSitting || User.RidingHorse || User.IsWalking)
                return;

            var Items = Room.GetGameMap().GetAllRoomItemForSquare(User.X, User.Y);
            bool HasBed = Items.ToList().Where(x => x != null && x.GetBaseItem().IsBed()).Count() > 0;

            if (HasBed || TargetSession.GetHabbo().VIPRank > 2)
                return;

            if (TargetSession.GetHabbo().Effects().CurrentEffect > 0)
                TargetSession.GetHabbo().Effects().ApplyEffect(0);

            if (!User.Statusses.ContainsKey("lay"))
            {
                if ((User.RotBody % 2) == 0)
                {
                    if (User == null)
                        return;

                    try
                    {
                        User.Statusses.Add("lay", "1.0 null");
                        User.Z -= 0.35;
                        User.isLying = true;
                        User.UpdateNeeded = true;
                    }
                    catch { }
                }
                else
                {
                    User.RotBody--;
                    User.Statusses.Add("lay", "1.0 null");
                    User.Z -= 0.35;
                    User.isLying = true;
                    User.UpdateNeeded = true;
                }

                User.Frozen = true;
                User.ClearMovement(true);
                User.ForceLay = true;

                PolarEnvironment.GetGame().GetChatManager().GetCommands().Parse(User.GetClient(), ":unequip");
                Session.Shout("*Looks into " + User.GetClient().GetHabbo().Username + "'s eyes causing them to collapse*", 23);

            }
            else
            {
                User.Z += 0.35;
                User.RemoveStatus("lay");
                User.isLying = false;
                User.UpdateNeeded = true;

                User.Frozen = false;
                User.ClearMovement(true);
                User.ForceLay = false;
               
                Session.Shout("*Mira adentro " + User.GetClient().GetHabbo().Username + "'s eyes causing them to stand up in fear*", 23);
            }
        }
    }
}
