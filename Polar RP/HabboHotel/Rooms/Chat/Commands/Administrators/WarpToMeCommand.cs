using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class WarpToMeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_warp_to_me"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Te teleporta a un usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduce el nombre de usuario del usuario que deseas.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no estén en línea o en esta sala.", 1);
                return;
            }

            if (TargetClient.GetHabbo().Username == Session.GetHabbo().Username)
            {
                Session.SendWhisper("¡No puedes deformarte!", 1);
                return;
            }

            var Point = new System.Drawing.Point(Session.GetRoomUser().X, Session.GetRoomUser().Y);
            var TargetPoint = new System.Drawing.Point(TargetUser.X, TargetUser.Y);

            if (Point == TargetPoint)
            {
                Session.SendWhisper("Esta persona ya está encima de ti!", 1);
                return;
            }

            TargetUser.ClearMovement(true);

            if (TargetUser.TeleportEnabled)
                TargetUser.MoveTo(Point);
            else
            {
                TargetUser.TeleportEnabled = true;
                TargetUser.MoveTo(Point);
                TargetUser.TeleportEnabled = false;
            }
            Session.Shout("*Utiliza sus poderes divinos y les urge " + TargetClient.GetHabbo().Username + "*", 23);
            return;
        }
    }
}