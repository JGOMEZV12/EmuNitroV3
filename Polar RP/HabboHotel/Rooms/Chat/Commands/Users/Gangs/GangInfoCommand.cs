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
    class GangInfoCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_gang_info"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Proporciona una lista de tu información de pandillas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);

            if (Gang == null || Session.GetRoleplay().GangId <= 1000)
            {
                Session.SendWhisper("No eres parte de una pandilla", 1);
                return;
            }

            int ScoreRanking = GroupManager.Gangs.Count;
            int KillRanking = GroupManager.Gangs.Count;
            int DeathRanking = GroupManager.Gangs.Count;

            lock (GroupManager.Gangs)
            {
                List<Group> GangList = GroupManager.Gangs.Values.Where(x => x.Id > 1000).ToList();
                ScoreRanking = GangList.OrderByDescending(x => x.GangScore).ToList().FindIndex(x => x.Id == Gang.Id);
                KillRanking = GangList.OrderByDescending(x => x.GangKills).ToList().FindIndex(x => x.Id == Gang.Id);
                DeathRanking = GangList.OrderBy(x => x.GangDeaths).ToList().FindIndex(x => x.Id == Gang.Id);

                StringBuilder Message = new StringBuilder();
                Message.Append("----- " + Gang.Name + " -----\n\n");
                Message.Append("Asesinatos: " + String.Format("{0:N0}", Gang.GangKills) + " ----- Clasificado " + String.Format("{0:N0}", KillRanking) + " fuera de " + String.Format("{0:N0}", GangList.Count) + "\n");
                Message.Append("Muertes: " + String.Format("{0:N0}", Gang.GangDeaths) + "  ----- Clasificado " + String.Format("{0:N0}", DeathRanking) + " fuera de " + String.Format("{0:N0}", GangList.Count) + "\n");
                Message.Append("Puntuación: " + String.Format("{0:N0}", Gang.GangScore) + " ----- Clasificado " + String.Format("{0:N0}", ScoreRanking) + " fuera de " + String.Format("{0:N0}", GangList.Count) + "\n\n");
                Message.Append("Dinero de pandilla: " + Gang.Balance + "\n");
                Message.Append("Paquetes médicos " + Gang.MediPacks + "\n\n");

                Message.Append("Fundado por: " + (PolarEnvironment.GetHabboById(Gang.CreatorId) == null ? PolarEnvironment.GetConfig().data["hotel.name"] : PolarEnvironment.GetHabboById(Gang.CreatorId).Username) + "\n\n");
           
                if (Gang.Members.Values.Where(x => x.UserRank == 5).ToList().Count > 0)
                {
                    GroupMember Member = Gang.Members.Values.FirstOrDefault(x => x.UserRank == 5);
                    Message.Append("Encargado: " + (PolarEnvironment.GetHabboById(Member.UserId) == null ? "Nadie" : PolarEnvironment.GetHabboById(Member.UserId).Username) + "\n");
                }
                else
                    Message.Append("Encargado por: Nadie\n");

                if (Gang.Members.Values.Where(x => x.UserRank == 4).ToList().Count > 0)
                {
                    List<int> Members = Gang.Members.Values.Where(x => x.UserRank == 4).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").ToList();
                    Message.Append("Médicos: " + String.Join(", ", Names) + "\n");
                }
                else
                    Message.Append("Médicos: Ninguno\n");

                if (Gang.Members.Values.Where(x => x.UserRank == 3).ToList().Count > 0)
                {
                    List<int> Members = Gang.Members.Values.Where(x => x.UserRank == 3).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").ToList();
                    Message.Append("Traficante de drogas: " + String.Join(", ", Names) + "\n");
                }
                else
                    Message.Append("Traficante de drogas: Ninguno\n");

                if (Gang.Members.Values.Where(x => x.UserRank == 2).ToList().Count > 0)
                {
                    List<int> Members = Gang.Members.Values.Where(x => x.UserRank == 2).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").ToList();
                    Message.Append("Ladrones: " + String.Join(", ", Names) + "\n");
                }
                else
                    Message.Append("Ladrones: Ninguno\n");

                if (Gang.Members.Values.Where(x => x.UserRank == 1).ToList().Count > 0)
                {
                    List<int> Members = Gang.Members.Values.Where(x => x.UserRank == 1).Select(x => x.UserId).ToList();
                    List<string> Names = Members.Select(x => PolarEnvironment.GetHabboById(x) == null ? "SKIPTHIS" : PolarEnvironment.GetHabboById(x).Username).Where(x => x != "SKIPTHIS").Take(10).ToList();
                    Message.Append("Primeros 10 novatos: " + String.Join(", ", Names) + "\n");
                }
                else
                    Message.Append("Primeros 10 novatos: Ninguno\n");

                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            }
        }
    }
}