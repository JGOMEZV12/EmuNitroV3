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
    internal class GetGroupForumThreadRootMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int StartIndex = Packet.PopInt();
            int EndIndex = Packet.PopInt();

            Group Group = GroupManager.GetJob(GroupId);
            if (Group == null || !Group.ForumEnabled) return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE group_id = @gid AND parent_id = '0' ORDER BY timestamp DESC");
                dbClient.AddParameter("gid", GroupId);
                DataTable Table = dbClient.getTable();

                if (Table == null)
                {
                    // ✅ Nuevo constructor: (Group, startIndex, threads)
                    Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, StartIndex, new List<GroupForumPost>()));
                    return;
                }

                int b = Math.Min(Table.Rows.Count, 20);
                var Threads = new List<GroupForumPost>();
                for (int i = 0; i < b; i++)
                {
                    DataRow row = Table.Rows[i];
                    if (row != null) Threads.Add(new GroupForumPost(row));
                }

                // El ordenamiento lo hace el composer internamente
                Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, StartIndex, Threads));
            }
        }
    }
}