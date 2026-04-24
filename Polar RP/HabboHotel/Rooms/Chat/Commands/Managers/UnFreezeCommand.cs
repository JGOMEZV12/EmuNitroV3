using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class UnFreezeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_freeze_undo"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Permitir que otro usuario camine de nuevo."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese el nombre de usuario del usuario que desea descongelar.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);
            if (TargetUser == null || TargetUser.GetClient() == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (!TargetUser.Frozen)
            {
                Session.SendWhisper("¡Este usuario no está congelado!", 1);
                return;
            }

            TargetUser.Frozen = false;

            if (TargetUser.CurrentEffect != 0)
                TargetUser.ApplyEffect(0);

            Session.Shout("*Utiliza sus poderes divinos y descongela a " + TargetUser.GetClient().GetHabbo().Username + "*", 23);
            Session.SendWhisper("Éxito al descongelar a " + TargetUser.GetClient().GetHabbo().Username + "!", 1);
            return;
        }
    }
}
