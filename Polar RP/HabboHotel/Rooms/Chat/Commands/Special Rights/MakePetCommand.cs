using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class MakePetCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_pet_transform"; }
        }

        public string Parameters
        {
            get { return "%user% %pet%"; }
        }

        public string Description
        {
            get { return "Permite transformar un usuario en una mascota."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("Sintaxis de comando incorrecto! :makepet <user> <pet>", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Lo sentimos, este usuario no se pudo encontrar!", 1);
                return;
            }

            if (TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("Lo siento, este usuario no está en la misma habitación que usted!", 1);
                return;
            }

            int TargetPetId;
            if (!int.TryParse(Params[2], out TargetPetId))
                TargetPetId = RoleplayManager.GetPetIdByString(Params[2].ToString());

            if (TargetPetId == 0)
            {
                Session.SendWhisper("¡Uy, no podría encontrar una mascota con ese nombre!", 1);
                return;
            }

            //Change the users Pet Id.         
            TargetClient.GetHabbo().PetId = (TargetPetId == -1 ? 0 : TargetPetId);

            //Quickly remove the old user instance.
            TargetClient.GetRoomUser().GetRoom().SendMessage(new UserRemoveComposer(TargetClient.GetRoomUser().VirtualId));

            //Add the new one, they won't even notice a thing!!11 8-)
            TargetClient.GetRoomUser().GetRoom().SendMessage(new UsersComposer(TargetClient.GetRoomUser()));

            //Tell them a quick message.
            if (TargetClient.GetHabbo().PetId > 0)
            {
                Session.Shout("*Utiliza sus poderes divinos y transforma a " + TargetClient.GetHabbo().Username + " en un " + Params[2].ToString() + "*", 23);
                TargetClient.SendWhisper("Un administrador te transformó en un " + Params[1].ToString(), 1);
            }
        }
    }
}