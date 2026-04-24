using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class PasajeroCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_pet_transform"; }
        }

        public string Parameters
        {
            get { return "%type%"; }
        }

        public string Description
        {
            get { return "Permite transformar un usuario en una mascota."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            int SessionPetId;
            if (!int.TryParse(Params[2], out SessionPetId))
                SessionPetId = RoleplayManager.GetPetIdByString(Params[2].ToString());

            //Change the users Pet Id.         
            Session.GetHabbo().PetId = (SessionPetId == -1 ? 0 : SessionPetId);

            //Quickly remove the old user instance.
            Session.GetRoomUser().GetRoom().SendMessage(new UserRemoveComposer(Session.GetRoomUser().VirtualId));

            //Add the new one, they won't even notice a thing!!11 8-)
            Session.GetRoomUser().GetRoom().SendMessage(new UsersComposer(Session.GetRoomUser()));

            //Tell them a quick message.
            if (Session.GetHabbo().PetId > 0)
            {
                Session.Shout("* Comienza a manejar su vehiculo y acepta pasajeros*", 4);
                Session.GetRoleplay().DrivingCar = true;
            }
        }
    }
}