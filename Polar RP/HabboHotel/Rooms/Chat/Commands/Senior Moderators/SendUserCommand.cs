using System;
using System.Linq;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Session;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class SendUserCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_send_user"; }
        }

        public string Parameters
        {
            get { return "%username% %roomid%"; }
        }

        public string Description
        {
            get { return "enviar a un usuario a una sala."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("el comando es asi:':enviar (user) (roomid)'.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetHabbo() == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().CurrentRoom != null)
            {
                if (TargetClient.GetHabbo().CurrentRoom.TutorialEnabled)
                {
                    Session.SendWhisper("¡No puedes enviar a alguien que esté dentro de una sala de tutoría!", 1);
                    return;
                }
            }

            int RoomId;
            if (!int.TryParse(Params[2], out RoomId))
            {
                Session.SendWhisper("Ingrese un ID de habitación válido", 1);
                return;
            }

            if (!HabboRoleplay.Misc.RoleplayManager.GenerateRoom(RoomId, out Room TargetRoom))
            {
                Session.SendWhisper("Lo sentimos, pero esta sala no se pudo encontrar", 1);
                return;
            }

            if (TargetClient.GetRoleplay().IsDead)
            {
                TargetClient.GetRoleplay().IsDead = false;
                TargetClient.GetRoleplay().ReplenishStats(true);
                TargetClient.GetHabbo().Poof();
            }

            if (TargetClient.GetRoleplay().IsJailed)
            {
                TargetClient.GetRoleplay().IsJailed = false;
                TargetClient.GetRoleplay().JailedTimeLeft = 0;
            }


            Session.Shout("*Utiliza sus poderes y envía " + TargetClient.GetHabbo().Username + " a " + TargetRoom.Name + " [RoomID: " + RoomId + "]*", 23);
            RoleplayManager.SendUserOld2(TargetClient, RoomId, "Te han enviado a la habitación " + TargetRoom.Name + " [RoomID: " + RoomId + "] por " + Session.GetHabbo().Username + "!");
        }
    }
}