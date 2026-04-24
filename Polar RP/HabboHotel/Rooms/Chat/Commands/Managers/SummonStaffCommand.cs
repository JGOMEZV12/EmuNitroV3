using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class SummonStaffCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_summon_staff"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Trae a todos los staff."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("coloca un mensaje.", 1);
                return;
            }

            List<string> CantSummon = new List<string>();

            int OnlineStaff = 0;

            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client == Session)
                    continue;

                if (client.GetHabbo().CurrentRoom == Room)
                    continue;

                if (!client.GetHabbo().GetPermissions().HasRight("mod_tool"))
                    continue;

                OnlineStaff++;


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

                RoleplayManager.SendUserOld2(client, Room.Id, "Te trajo a la sala " + Session.GetHabbo().Username + "");
            }
            if (OnlineStaff > 0)
                Session.Shout("*Utiliza sus poderes divinos y convoca a todos los miembros del personal en línea a la habitación*", 23);
            else
            {
                Session.SendWhisper("¡Lo siento! ¡No hay otro personal online ahora mismo!", 1);
                return;
            }

            if (CantSummon.Count > 0)
            {
                string Users = "";

                foreach (string user in CantSummon)
                {
                    Users += user + ",";
                }

                Session.SendMessage(new MOTDNotificationComposer("Lo siento, no pude convocar a los siguientes miembros del personal ya que están dentro de un evento\n\n " + Users));
            }
        }
    }
}
