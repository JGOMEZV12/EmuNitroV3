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
    internal class GetGroupForumThreadRootMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int StartIndex = Packet.PopInt();
            int EndIndex = Packet.PopInt();

            Group Group = GroupManager.GetJob(GroupId);

            if (Group == null || !Group.ForumEnabled)
                return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `groups_forums_posts` WHERE `group_id` = @gid AND `parent_id` = '0' ORDER BY timestamp DESC");
                dbClient.AddParameter("gid", GroupId);

                DataTable Table = dbClient.getTable();

                if (Table == null)
                {
                    Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, 1, 0, 0, null, Session));
                    return;
                }

                int b = (Table.Rows.Count <= 20) ? Table.Rows.Count : 20;
                var Threads = new List<GroupForumPost>();

                int i = 1;

                while (i <= b)
                {
                    DataRow Row = Table.Rows[i - 1];

                    if (Row == null)
                    {
                        b--;
                        continue;
                    }

                    var thread = new GroupForumPost(Row);

                    Threads.Add(thread);

                    i++;
                }

                Threads = Threads.OrderByDescending(x => x.Pinned).ToList();

                Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, 2, StartIndex, b, Threads, Session));
            }
        }
    }
}
