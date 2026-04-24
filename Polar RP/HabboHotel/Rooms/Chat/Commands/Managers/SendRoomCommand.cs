using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class SendRoomCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_send_room"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Envía a todos los usuarios en la misma habitación que a la ID de la habitación deseada."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor ingrese un id de habitación para enviar a los usuarios!", 1);
                return;
            }

            if (Room.GetRoomUserManager().GetRoomUsers().Count == 1)
            {
                Session.SendWhisper("Usted es la única persona en la sala!", 1);
                return;
            }

            int RoomId;
            if (!int.TryParse(Params[1], out RoomId))
            {
                Session.SendWhisper("Ingrese un ID de habitación válido!", 1);
                return;
            }

            if (!HabboRoleplay.Misc.RoleplayManager.GenerateRoom(RoomId, out Room TargetRoom))
            {
                Session.SendWhisper("Lo sentimos, pero esta sala no se pudo encontrar!", 1);
                return;
            }

            if (TargetRoom == Session.GetHabbo().CurrentRoom)
            {
                Session.SendWhisper("Usted y todos los demás ya están en esa habitación!", 1);
                return;
            }

            List<string> CantSend = new List<string>();

            int count = 0;
            foreach (var user in Room.GetRoomUserManager().GetRoomUsers())
            {
                if (user == null || user.GetClient() == null || user.GetClient().GetHabbo() == null)
                    continue;

                if (user.GetClient() == Session)
                    continue;

                count++;

                if (user.GetClient().GetRoleplay().IsDead)
                {
                    user.GetClient().GetRoleplay().IsDead = false;
                    user.GetClient().GetRoleplay().ReplenishStats(true);
                    user.GetClient().GetHabbo().Poof();
                }

                if (user.GetClient().GetRoleplay().IsJailed)
                {
                    user.GetClient().GetRoleplay().IsJailed = false;
                    user.GetClient().GetRoleplay().JailedTimeLeft = 0;
                }

                RoleplayManager.SendUserOld2(user.GetClient(), RoomId, "Te han enviado a la habitación " + TargetRoom.Name + " [RoomID: " + RoomId + "] por " + Session.GetHabbo().Username + "!");
            }

            if (count > 0)
                Session.Shout("*Utiliza sus poderes divinos y envía a todos en la sala a " + TargetRoom.Name + " [RoomID: " + RoomId + "]*", 23);

            if (CantSend.Count > 0)
            {
                string Users = "";

                foreach (string user in CantSend)
                {
                    Users += user + ",";
                }

                Session.SendMessage(new MOTDNotificationComposer("Lo sentimos, no pudimos enviar a los siguientes usuarios fuera de la sala ya que están dentro de un Evento!\n\n " + Users));
            }
        }
    }
}
