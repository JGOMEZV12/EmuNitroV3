using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class RoomMakePetCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_room_make_pet"; }
        }

        public string Parameters
        {
            get { return "%pet%"; }
        }

        public string Description
        {
            get { return "Allows you to transform everybody in the room into a pet."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length < 2)
            {
                Session.SendWhisper("Incorrect command syntax! :rmakepet <pet>", 1);
                return;
            }

            int TargetPetId;
            if (!int.TryParse(Params[1], out TargetPetId))
                TargetPetId = RoleplayManager.GetPetIdByString(Params[1].ToString());

            if (TargetPetId == 0)
            {
                Session.SendWhisper("Oops, couldn't find a pet by that name!", 1);
                return;
            }
            #endregion

            #region Execute
            lock (Room.GetRoomUserManager().GetRoomUsers())
            {
                foreach (var user in Room.GetRoomUserManager().GetRoomUsers())
                {
                    if (user == null || user.IsBot || user.IsPet || user.GetClient() == null || user.GetClient().GetHabbo() == null)
                        continue;

                    if (user.GetClient().GetHabbo().PetId == TargetPetId)
                        continue;

                    //Change the users Pet Id.
                    user.GetClient().GetHabbo().PetId = (TargetPetId == -1 ? 0 : TargetPetId);

                    //Quickly remove the old user instance.
                    user.GetRoom().SendMessage(new UserRemoveComposer(user.VirtualId));

                    //Add the new one, they won't even notice a thing!!11 8-)
                    user.GetRoom().SendMessage(new UsersComposer(user));

                    //Tell them a quick message.
                    if (user.GetClient().GetHabbo().PetId > 0)
                        user.GetClient().SendWhisper("An admin transformed you into a " + Params[1].ToString(), 1);
                }
                Session.Shout("*Utiliza sus poderes divinos and transforms everybody in the room into a " + Params[1].ToString() + "*", 23);
            }
            #endregion
        }
    }
}