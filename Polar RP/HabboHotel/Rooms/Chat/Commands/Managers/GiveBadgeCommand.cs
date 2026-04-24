using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class GiveBadgeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_badge"; }
        }

        public string Parameters
        {
            get { return "%username% %badge%"; }
        }

        public string Description
        {
            get { return "Dar una placa a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("Please enter a username and the code of the badge you'd like to give!", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            Badges.BadgeDefinition BadgeDefinition = null;
            if (!PolarEnvironment.GetGame().GetBadgeManager().TryGetBadge(Params[2].ToUpper(), out BadgeDefinition))
            {
                Session.SendWhisper("The badge definitions do not contain this badge!", 1);
                return;
            }

            if (TargetClient != null)
            {
                if (!TargetClient.GetHabbo().GetBadgeComponent().HasBadge(Params[2]))
                {
                    TargetClient.GetHabbo().GetBadgeComponent().GiveBadge(Params[2], true, TargetClient);
                    if (TargetClient.GetHabbo().Id != Session.GetHabbo().Id)
                    {
                        TargetClient.SendNotification("You have just been given a badge!");
                        Session.SendWhisper("You have successfully given " + TargetClient.GetHabbo().Username + " the badge " + Params[2].ToUpper() + "!", 1);
                    }
                    else
                        Session.SendWhisper("You have successfully given yourself the badge " + Params[2].ToUpper() + "!", 1);
                }
                else
                    Session.SendWhisper("Oops, that user already has the badge (" + Params[2].ToUpper() + ") !", 1);
                return;
            }
            else
            {
                Session.SendWhisper("Oops, we couldn't find that target user!", 1);
                return;
            }
        }
    }
}
