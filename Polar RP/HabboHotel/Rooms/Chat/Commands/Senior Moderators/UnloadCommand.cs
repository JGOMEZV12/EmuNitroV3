using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class UnloadCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_unload"; }
        }

        public string Parameters
        {
            get { return "%id%"; }
        }

        public string Description
        {
            get { return "Unload the current room."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Room R = null;
            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Room.Id, out R))
                return;

            int RoomId = Session.GetHabbo().CurrentRoomId;

            if (Room.Name.ToLower().Contains("gun"))
            {
                RoleplayManager.CalledDelivery = false;
                RoleplayManager.DeliveryWeapon = null;
            }

            List<RoomUser> UsersToReturn = Room.GetRoomUserManager().GetRoomUsers().ToList();

            PolarEnvironment.GetGame().GetRoomManager().UnloadRoom(R, true);

            

            foreach (RoomUser User in UsersToReturn)
            {
                if (User == null || User.GetClient() == null)
                    continue;

                RoleplayManager.SendUserOld2(User.GetClient(), RoomId, "Se ha refrescado la sala");
				//User.GetClient().SendMessage(new RoomForwardComposer(Room.Id));

            }
        }
    }
}
