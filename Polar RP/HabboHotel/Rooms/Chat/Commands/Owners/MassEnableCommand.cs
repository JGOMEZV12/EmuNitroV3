using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class MassEnableCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_enable_mass"; }
        }

        public string Parameters
        {
            get { return "%EffectId%"; }
        }

        public string Description
        {
            get { return "Poner en todos un efecto."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Ingrese un ID de efecto válido.", 1);
                return;
            }

            int EnableId = 0;
            if (!int.TryParse(Params[1], out EnableId))
            {
                Session.SendWhisper("Ingrese un ID de efecto válido.", 1);
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
                if (U == null || U.RidingHorse)
                    continue;

                if (U.CarryItemID > 0)
                    U.CarryItem(0);

                if (U.DanceId > 0)
                    U.DanceId = 0;

                U.ApplyEffect(EnableId);
            }
        
            Session.Shout("*Utiliza sus poderes divinos y cambia el efecto a todos*", 23);
        }
    }
}
