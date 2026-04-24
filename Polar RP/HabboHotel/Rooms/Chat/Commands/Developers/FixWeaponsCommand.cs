using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class FixWeaponsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_fixweapons"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Recarga la lista de armas de los usuarios."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca el nombre de usuario del usuario en el que desea utilizar este comando.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea.", 1);
                return;
            }

            RoomUser TargetUser = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);

            if (TargetClient.GetRoleplay().EquippedWeapon != null)
                TargetClient.GetRoleplay().EquippedWeapon = null;

            TargetClient.GetRoleplay().OwnedWeapons = null;
            TargetClient.GetRoleplay().OwnedWeapons = TargetClient.GetRoleplay().LoadAndReturnWeapons();

            Session.Shout("*Utiliza sus poderes divinos y refresca " + TargetClient.GetHabbo().Username + "'s weapons*", 23);
            TargetClient.SendWhisper("Sus armas han sido refrescadas por " + Session.GetHabbo().Username + "!", 1);
        }
    }
}
