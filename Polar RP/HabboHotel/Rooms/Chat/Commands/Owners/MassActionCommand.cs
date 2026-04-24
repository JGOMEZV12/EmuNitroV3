using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class MassActionCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_action_mass"; }
        }

        public string Parameters
        {
            get { return "%actionid%"; }
        }

        public string Description
        {
            get { return "Obligar a todos a realizar una acción."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese un ID de acción válido.", 1);
                return;
            }

            int ActionId;
            if (!int.TryParse(Params[1], out ActionId))
            {
                Session.SendWhisper("Ingrese un ID de acción válido.", 1);
                return;
            }

            if (ActionId == 5)
            {
                Session.SendWhisper("Lo siento, no puedes hacer que todo el mundo vaya ocioso!", 1);
                return;
            }

            List<RoomUser> Users = Room.GetRoomUserManager().GetRoomUsers();

            if (Users.Count <= 1)
            {
                Session.SendWhisper("Usted es la única persona en la sala!", 1);
                return;
            }

            foreach (RoomUser U in Users.ToList())
            {
                if (U == null)
                    continue;

                if (U.CarryItemID > 0)
                    U.CarryItemID = 0;

                if (U.DanceId > 0)
                    U.DanceId = 0;

                if (U.CurrentEffect > 0)
                    U.ApplyEffect(0);

                Room.SendMessage(new ActionComposer(U.VirtualId, ActionId));
            }

            Session.Shout("*UUtiliza sus poderes divinos y obliga a todos a realizar una acción*", 23);
        }
    }
}
