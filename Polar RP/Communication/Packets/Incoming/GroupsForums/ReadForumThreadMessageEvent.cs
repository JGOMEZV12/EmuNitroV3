using System;
using System.Data;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class ReadForumThreadMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int ThreadId = Packet.PopInt();
            int StartIndex = Packet.PopInt();
            int StopIndex = Packet.PopInt();

            Group Group = GroupManager.GetJob(GroupId);
            if (Group == null || !Group.ForumEnabled) return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE group_id = @groupid AND parent_id = @threadid OR id = @threadid ORDER BY timestamp ASC");
                dbClient.AddParameter("groupid", GroupId);
                dbClient.AddParameter("threadid", ThreadId);
                DataTable Table = dbClient.getTable();
                if (Table == null) return;

                int b = Math.Min(Table.Rows.Count, 20);
                var posts = new List<GroupForumPost>();
                for (int i = 0; i < b; i++)
                {
                    DataRow row = Table.Rows[i];
                    if (row == null) continue;
                    var post = new GroupForumPost(row);
                    if (post.ParentId == 0 && post.Hidden) return;
                    posts.Add(post);
                }

                // ✅ Nuevo constructor: (groupId, threadId, startIndex, posts)
                Session.SendMessage(new GroupForumReadThreadMessageComposer(GroupId, ThreadId, StartIndex, posts));
            }
        }
    }
}