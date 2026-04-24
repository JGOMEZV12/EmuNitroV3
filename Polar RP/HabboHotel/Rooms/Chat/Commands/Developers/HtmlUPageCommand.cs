using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class HtmlUPageCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_open_div"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Opens a page for user by their id ( stop sumo )"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("Please enter the users name followed by a page to send.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetRoleplay() == null)
            {
                Session.SendWhisper("Couldn't find this user!", 1);
                return;
            }

            if (TargetClient.GetRoleplay().WebSocketConnection == null)
            {
                Session.SendWhisper("We cannot alert this user at this time! Their socket connection returned null!", 1);
                return;
            }

            string Data = "action:broadcast,page:" + Params[2].ToLower();
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_htmlpage", Data);
            Session.SendWhisper("Opened PAGE: '" + Params[2].ToLower() + "' for " + TargetClient.GetHabbo().Username, 1);
            return;
        }
    }
}
