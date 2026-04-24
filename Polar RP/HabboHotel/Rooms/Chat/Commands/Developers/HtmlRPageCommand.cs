using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class HtmlRPageCommand : IChatCommand
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
            get { return "Opens page for room"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Please enter the page you want to open for the entire hotel!", 1);
                return;
            }

            if (Room != null && Room.GetRoomUserManager() != null && Room.GetRoomUserManager().GetRoomUsers() != null)
            {
                lock (Room.GetRoomUserManager().GetRoomUsers().ToList())
                {
                    foreach (RoomUser User in Room.GetRoomUserManager().GetRoomUsers().ToList())
                    {
                        if (User == null)
                            continue;

                        if (User.IsBot)
                            continue;

                        if (User.GetClient() == null)
                            continue;

                        if (User.GetClient().LoggingOut)
                            continue;

                        if (User.GetClient().GetRoleplay() == null)
                            continue;

                        if (User.GetClient().GetRoleplay().WebSocketConnection == null)
                            continue;

                        string Data = "action:broadcast,page:" + Params[1].ToLower();
                        PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(User.GetClient(), "event_htmlpage", Data);
                    }
                    Session.SendWhisper("Successfully sent " + Params[1].ToLower() + " page to room!", 1);
                    return;
                }
            }
            else
            {
                Session.SendWhisper("An error occurred! The room must've returned null!", 1);
                return;
            }
        }
    }
}
