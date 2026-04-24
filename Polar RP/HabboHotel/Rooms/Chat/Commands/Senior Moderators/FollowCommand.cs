using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class FollowCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_follow"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Sigue a un usuario"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Coloca el nombre del usuario que deseas seguir", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().CurrentRoom == Session.GetHabbo().CurrentRoom)
            {
                Session.SendWhisper(TargetClient.GetHabbo().Username + " Está en la misma habitación que tú", 1);
                return;
            }

            if (TargetClient.GetHabbo().Username == Session.GetHabbo().Username)
            {
                Session.SendWhisper("¡No puedes seguirte!", 1);
                return;
            }

            if (!TargetClient.GetHabbo().InRoom)
            {
                Session.SendWhisper("Ese usuario actualmente no está en una habitación", 1);
                return;
            }

            Session.Shout("*Utiliza sus poderes divinos y sigue a " + TargetClient.GetHabbo().Username + "*", 23);
            RoleplayManager.SendUserOld2(Session, TargetClient.GetHabbo().CurrentRoomId);
        }
    }
}
