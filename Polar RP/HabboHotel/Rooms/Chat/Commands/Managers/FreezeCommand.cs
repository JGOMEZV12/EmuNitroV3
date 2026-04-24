using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class FreezeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_freeze"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Evite que otro usuario camine."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca el nombre de usuario del usuario que desea congelar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            RoomUser TargetUser = Session.GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Params[1]);

            if (TargetClient.GetRoleplay().EquippedWeapon != null)
                TargetClient.GetRoleplay().EquippedWeapon = null;

            if (TargetUser != null)
            {
                TargetUser.Frozen = true;
                TargetUser.ClearMovement(true);
                TargetUser.ApplyEffect(EffectsList.Ice);
            }

            Session.Shout("*Utiliza sus poderes divinos y congela a " + TargetClient.GetHabbo().Username + "*", 23);
            Session.SendWhisper("Se congeló con éxito a " + TargetClient.GetHabbo().Username + "!", 1);
        }
    }
}
