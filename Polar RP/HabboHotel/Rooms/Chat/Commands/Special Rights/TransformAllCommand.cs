using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class TransformAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_transform_all"; }
        }

        public string Parameters
        {
            get { return "%pet%"; }
        }

        public string Description
        {
            get { return "Allows you to transform everybody on the hotel into a pet."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length < 2)
            {
                Session.SendWhisper("Incorrect command syntax! :transformall <pet>", 1);
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
            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
            {
                foreach (var Client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null || Client.GetRoomUser() == null)
                        continue;

                    if (Client.GetHabbo().PetId == TargetPetId)
                        continue;

                    //Change the Clients Pet Id.
                    Client.GetHabbo().PetId = (TargetPetId == -1 ? 0 : TargetPetId);

                    //Quickly remove the old Client instance.
                    Client.GetRoomUser().GetRoom().SendMessage(new UserRemoveComposer(Client.GetRoomUser().VirtualId));

                    //Add the new one, they won't even notice a thing!!11 8-)
                    Client.GetRoomUser().GetRoom().SendMessage(new UsersComposer(Client.GetRoomUser()));

                    //Tell them a quick message.
                    if (Client.GetHabbo().PetId > 0)
                        Client.SendWhisper("An admin transformed you into a " + Params[1].ToString(), 1);
                }
                Session.Shout("*Utiliza sus poderes divinos and transforms everybody in the hotel into a " + Params[1].ToString() + "*", 23);
            }
            #endregion
        }
    }
}