using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class SummonAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_summon_all"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Convocar a todos en una sala."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            List<string> CantSummon = new List<string>();

            int OnlineUsers = 0;

            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client == Session)
                    continue;

                if (client.GetHabbo().CurrentRoom != null)
                {
                    if (client.GetHabbo().CurrentRoom == Room)
                        continue;

                    if (client.GetHabbo().CurrentRoom.TutorialEnabled)
                        continue;
                }

                OnlineUsers++;

                if (client.GetRoleplay().IsDead)
                {
                    client.GetRoleplay().IsDead = false;
                    client.GetRoleplay().ReplenishStats(true);
                    client.GetHabbo().Poof();
                }

                if (client.GetRoleplay().IsJailed)
                {
                    client.GetRoleplay().IsJailed = false;
                    client.GetRoleplay().JailedTimeLeft = 0;
                    client.GetHabbo().Poof();
                }
				

                RoleplayManager.SendUserOld2(client, Room.Id, "Usted ha sido convocado por" + Session.GetHabbo().Username + "!");
            }
            if (OnlineUsers > 0)
                Session.Shout("*Utiliza sus poderes divinos y convoca a todos los usuarios en línea a la habitación*", 23);
            else
            {
                Session.SendWhisper("¡Lo siento! ¡No hay otros usuarios en línea ahora mismo!", 1);
                return;
            }

            if (CantSummon.Count > 0)
            {
                string Users = "";

                foreach (string user in CantSummon)
                {
                    Users += user + ",";
                }

                Session.SendMessage(new MOTDNotificationComposer("¡Lo sentimos, no pudimos convocar a los siguientes usuarios como están dentro de un Evento!\n\n " + Users));
            }
        }
    }
}
