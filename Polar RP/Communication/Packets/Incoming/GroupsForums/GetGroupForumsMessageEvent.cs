using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class GetGroupForumsMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int selectType = Packet.PopInt();
            int startIndex = Packet.PopInt();
            int endIndex = Packet.PopInt();

            List<Group> groupList = new List<Group>();

            switch (selectType)
            {
                case 0:
                case 1:
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT count(id) FROM `rp_jobs` WHERE forum_messages_count > 0");
                        int qtdForums = dbClient.getInteger();

                        dbClient.SetQuery("SELECT `id` FROM `rp_jobs` WHERE `forum_messages_count` > 0 ORDER BY `forum_messages_count` DESC LIMIT @startIndex, @totalPerPage;");
                        dbClient.AddParameter("startIndex", startIndex);
                        dbClient.AddParameter("totalPerPage", endIndex);
                        DataTable table = dbClient.getTable();
                        groupList.AddRange(from DataRow rowGroupData in table.Rows select int.Parse(rowGroupData["id"].ToString()) into groupId select GroupManager.GetJob(groupId));
                        Session.SendMessage(new GroupForumListingsMessageComposer(selectType, qtdForums, startIndex, groupList));
                        break;
                    }
                case 2:
                    {
                        groupList.AddRange(PolarEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id).Where(x => x.Id < 1000 && x.ForumEnabled).ToList());
                        groupList = groupList.OrderByDescending(x => x.ForumMessagesCount).Skip(startIndex).Take(endIndex).ToList();
                        Session.SendMessage(new GroupForumListingsMessageComposer(selectType, groupList.Count, startIndex, groupList));
                        break;
                    }
                default:
                    {
                        Session.SendMessage(new GroupForumListingsMessageComposer(selectType, 0, startIndex, null));
                        break;
                    }
            }
        }
    }
}
