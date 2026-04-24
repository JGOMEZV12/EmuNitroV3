using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Avatar;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class MassDanceCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_dance_mass"; }
        }

        public string Parameters
        {
            get { return "%DanceId%"; }
        }

        public string Description
        {
            get { return "Obligar a todos en la sala a bailar a un baile de su elección."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese un ID de baile válido. (1-4)", 1);
                return;
            }

            int DanceId;
            if (!int.TryParse(Params[1], out DanceId))
            {
                Session.SendWhisper("Ingrese un ID de baile válido. (1-4)", 1);
                return;
            }

            if (DanceId < 0 || DanceId > 4)
            {
                Session.SendWhisper("Ingrese un ID de baile válido. (1-4)", 1);
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

                if (U.CurrentEffect > 0)
                    U.ApplyEffect(0);

                U.DanceId = DanceId;
                Room.SendMessage(new DanceComposer(U, DanceId));
            }

            Session.Shout("*Utiliza sus poderes divinos y obliga a todos a bailar*", 23);
        }
    }
}
