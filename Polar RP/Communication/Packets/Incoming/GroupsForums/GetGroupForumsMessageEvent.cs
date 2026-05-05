using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
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

            var groupList = new List<Group>();

            switch (selectType)
            {
                case 0:
                case 1:
                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("SELECT `id` FROM `rp_jobs` WHERE `forum_messages_count` > 0 ORDER BY `forum_messages_count` DESC LIMIT @startIndex, @totalPerPage");
                        dbClient.AddParameter("startIndex", startIndex);
                        dbClient.AddParameter("totalPerPage", endIndex);
                        DataTable table = dbClient.getTable();
                        if (table != null)
                        {
                            foreach (DataRow row in table.Rows)
                            {
                                int gid = int.Parse(row["id"].ToString());
                                Group g = GroupManager.GetJob(gid);
                                if (g != null) groupList.Add(g);
                            }
                        }
                        // ✅ Nuevo constructor: (mode, startIndex, groups, session)
                        Session.SendMessage(new GroupForumListingsMessageComposer(selectType, startIndex, groupList, Session));
                        break;
                    }

                case 2:
                    groupList.AddRange(
                        PolarEnvironment.GetGame().GetGroupManager()
                            .GetGroupsForUser(Session.GetHabbo().Id)
                            .Where(x => x.Id < 1000 && x.ForumEnabled)
                            .OrderByDescending(x => x.ForumMessagesCount)
                            .Skip(startIndex)
                            .Take(endIndex));

                    Session.SendMessage(new GroupForumListingsMessageComposer(selectType, startIndex, groupList, Session));
                    break;

                default:
                    Session.SendMessage(new GroupForumListingsMessageComposer(selectType, startIndex, new List<Group>(), Session));
                    break;
            }
        }
    }
}