using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class CorpListCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_corp_list"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Provides you a list of all the available corporations."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            lock (GroupManager.Jobs)
            {
                List<Group> CorpList = GroupManager.Jobs.Values.Where(x => x.Id > 1).ToList();

                StringBuilder Message = new StringBuilder();
                Message.Append("---------- Empresas de " + PolarEnvironment.GetConfig().data["hotel.name"] + " ----------\n\n");

                foreach (Group Corp in CorpList)
                {
                    RoleplayManager.GenerateRoom(Corp.RoomId, out Room CorpRoom);

                    Message.Append("_______ " + Corp.Name + " [ID: " + Corp.Id + "] _______\n");
                    Message.Append("Descripción: " + Corp.Description + "\n");

                    if (CorpRoom != null)
                        Message.Append("Dirección: " + CorpRoom.Name + " [RoomID: " + CorpRoom.RoomId + "]\n");
                    else
                        Message.Append("Dirección: Desconocida [RoomID: N/A]\n");

                    if (Corp.Members.Values.Where(x => x.UserRank == 6).ToList().Count > 0)
                        Message.Append("Creada por: " + ((PolarEnvironment.GetHabboById(Corp.Members.Values.FirstOrDefault(x => x.UserRank == 6).UserId) != null) ? PolarEnvironment.GetHabboById(Corp.Members.Values.FirstOrDefault(x => x.UserRank == 6).UserId).Username : "HoloRP") + "\n\n");
                    else
                        Message.Append("Creada por: Gobierno\n\n");
                }

                Session.SendMessage(new MOTDNotificationComposer(Message.ToString()));
            }
        }
    }
}