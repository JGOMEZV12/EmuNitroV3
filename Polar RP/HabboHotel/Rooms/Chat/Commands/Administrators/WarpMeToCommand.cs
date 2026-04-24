using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class WarpMeToCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_warp_me_to"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Se teletransporta a otro usuario."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, ingrese el nombre de usuario del usuario al que desea enlazar.", 1);
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
                Session.SendWhisper("Usted no puede deformarse a sí mismo!", 1);
                return;
            }

            var Point = new System.Drawing.Point(Session.GetRoomUser().X, Session.GetRoomUser().Y);
            var TargetPoint = new System.Drawing.Point(TargetUser.X, TargetUser.Y);

            if (Point == TargetPoint)
            {
                Session.SendWhisper("Ya estás en la parte superior de este usuario!", 1);
                return;
            }

            Session.GetRoomUser().ClearMovement(true);

            if (Session.GetRoomUser().TeleportEnabled)
                Session.GetRoomUser().MoveTo(TargetPoint);
            else
            {
                Session.GetRoomUser().TeleportEnabled = true;
                Session.GetRoomUser().MoveTo(TargetPoint);
                Session.GetRoomUser().TeleportEnabled = false;
            }
            Session.Shout("Utiliza sus poderes divinos y se transmite encima de " + TargetClient.GetHabbo().Username + "*", 23);
            return;
        }
    }
}