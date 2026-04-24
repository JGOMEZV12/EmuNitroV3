using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Gangs
{
    class GangListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_list"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le proporciona una lista de las 10 mejores pandillas según el puntaje de pandillas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            lock (GroupManager.Gangs)
            {
                List<Group> GangList = GroupManager.Gangs.Values.Where(x => x.IsGang).OrderByDescending(x => x.GangScore).Take(10).ToList();

                StringBuilder Message = new StringBuilder();
                Message.Append("---------- Las 10 mejores pandillas ----------\n\n");

                foreach (Group Gang in GangList)
                {
                    Message.Append("----- " + Gang.Name + " -----\n");
                    Message.Append("Clasificado: " + (GangList.FindIndex(x => x.Id == Gang.Id) + 1) + " fuera de " + String.Format("{0:N0}", GangList.Count) + "\n");
                    Message.Append("Asesinatos: " + String.Format("{0:N0}", Gang.GangKills) + "\n");
                    Message.Append("Muertes: " + String.Format("{0:N0}", Gang.GangDeaths) + "\n");
                    Message.Append("Puntuación: " + String.Format("{0:N0}", Gang.GangScore) + "\n");
                    Message.Append("Fundado por: " + (PolarEnvironment.GetHabboById(Gang.CreatorId) == null ? PolarEnvironment.GetConfig().data["hotel.name"] : PolarEnvironment.GetHabboById(Gang.CreatorId).Username) + "\n\n");
                }

                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            }
        }
    }
}