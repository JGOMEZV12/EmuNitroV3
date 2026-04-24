using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class VisibleCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_invisible"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Makes you visible"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (!Session.GetRoleplay().Invisible)
            {
                Session.SendWhisper("You are already visible!", 1);
                return;
            }

            Session.GetHabbo().CurrentRoom.SendMessage(new UsersComposer(Session.GetRoomUser()));
            Session.SendWhisper("You are now visible!", 1);
            Session.GetRoleplay().Invisible = false;

            string cantsee = "";

            foreach (RoomUser invisibleuser in Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetUserList().ToList())
            {
                if (invisibleuser.IsBot)
                    continue;

                if (invisibleuser.GetClient().GetHabbo().Username != Session.GetHabbo().Username && invisibleuser.GetClient().GetRoleplay().Invisible)
                {
                    invisibleuser.GetClient().SendWhisper(Session.GetHabbo().Username + " just went visible, so they can no longer see you!", 1);
                    cantsee += invisibleuser.GetClient().GetHabbo().Username + ", ";
                    Session.SendMessage(new UserRemoveComposer(invisibleuser.VirtualId));
                }
            }


            Session.SendWhisper((cantsee == "" ? "There is no invisible people in the room!" : "You can NO LONGER see: " + cantsee + "as they are invisible!"), 1);

        }
    }
}