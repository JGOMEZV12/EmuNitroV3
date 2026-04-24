using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class PetTransformCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_pet_transform"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite transformarse en una mascota."; }
        }

        public async Task Execute(GameClients.GameClient Session, Room Room, string[] Params)
        {
            RoomUser RoomUser = Session.GetRoomUser();
            if (RoomUser == null)
                return;

            if (!Room.PetMorphsAllowed)
            {
                Session.SendWhisper("El propietario de la habitación ha desactivado la posibilidad de utilizar una mascota morph en esta habitación.", 1);
                if (Session.GetHabbo().PetId > 0)
                {
                    Session.SendWhisper("Oops, you still have a morph, un-morphing you.", 1);
                    //Change the users Pet Id.
                    Session.GetHabbo().PetId = 0;

                    //Quickly remove the old user instance.
                    Room.SendMessage(new UserRemoveComposer(RoomUser.VirtualId));

                    //Add the new one, they won't even notice a thing!!11 8-)
                    Room.SendMessage(new UsersComposer(RoomUser));
                }
                return;
            }

            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Uy, se te olvidó elegir el tipo de mascota en el que te gustaría convertirte! Usa ':pet list' para ver los pets disponibles!", 1);
                return;
            }

            if (Params[1].ToString().ToLower() == "list")
            {
                Session.SendWhisper("Habbo, Dog, Cat, Terrier, Croc, Bear, Pig, Lion, Rhino, Spider, Turtle, Chick, Frog, Drag, Monkey, Horse, Bunny, Pigeon, Demon and Gnome.", 1);
                return;
            }

            int TargetPetId;
            if (!int.TryParse(Params[1], out TargetPetId))
                TargetPetId = RoleplayManager.GetPetIdByString(Params[1].ToString());

            if (TargetPetId == 0)
            {
                Session.SendWhisper("Oops, no pudo encontrar una mascota con ese nombre!", 1);
                return;
            }

            //Change the users Pet Id.
            Session.GetHabbo().PetId = (TargetPetId == -1 ? 0 : TargetPetId);

            //Quickly remove the old user instance.
            Room.SendMessage(new UserRemoveComposer(RoomUser.VirtualId));

            //Add the new one, they won't even notice a thing!!11 8-)
            Room.SendMessage(new UsersComposer(RoomUser));

            //Tell them a quick message.
            if (Session.GetHabbo().PetId > 0)
            {
                Session.Shout("*Utiliza sus poderes divinos y se transforma en un " + Params[1].ToString() + "*", 23);
                Session.SendWhisper("Usa ':pet habbo' para volver a ser un ciudadano!", 1);
            }
            else
            {
                Session.Shout("*Utiliza sus poderes divinos y los transforma en un ciudadano*", 23);
            }
        }

    }
}