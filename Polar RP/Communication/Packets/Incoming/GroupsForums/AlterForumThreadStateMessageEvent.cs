using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.Communication.Packets.Outgoing.Groups;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.Communication.Packets.Incoming.Groups
{
    internal class AlterForumThreadStateMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int ThreadId = Packet.PopInt();
            int StateToSet = Packet.PopInt();

            Group Group = GroupManager.GetJob(GroupId);
            if (Group == null) return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE group_id = @gid AND id = @tid LIMIT 1");
                dbClient.AddParameter("gid", GroupId);
                dbClient.AddParameter("tid", ThreadId);
                DataRow Row = dbClient.getRow();
                if (Row == null) return;

                if (Convert.ToInt32(Row["poster_id"]) == Session.GetHabbo().Id || Group.IsAdmin(Session.GetHabbo().Id))
                {
                    string state = (StateToSet == 20 || StateToSet == 10) ? "1" : "0";
                    dbClient.SetQuery("UPDATE groups_forums_posts SET hidden = @hid, post_hider = @uid WHERE id = @tid");
                    dbClient.AddParameter("hid", state);
                    dbClient.AddParameter("uid", Session.GetHabbo().Id);
                    dbClient.AddParameter("tid", ThreadId);
                    dbClient.RunQuery();
                }

                var Thread = new GroupForumPost(Row);

                Session.SendMessage(new RoomNotificationComposer(
                    (StateToSet == 20 || StateToSet == 10) ? "forums.thread.hidden" : "forums.thread.restored"));

                if (Thread.ParentId != 0) return;

                // ✅ Nuevo constructor: (Group, Thread)
                Session.SendMessage(new GroupForumThreadUpdateMessageComposer(Group, Thread));

                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE group_id = @gid AND parent_id = 0 ORDER BY timestamp DESC");
                dbClient.AddParameter("gid", GroupId);
                DataTable Table = dbClient.getTable();

                if (Table == null)
                {
                    Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, 0, new List<GroupForumPost>()));
                    return;
                }

                var Threads = BuildThreadList(Table);
                // ✅ Nuevo constructor: (Group, startIndex, threads)
                Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, 0, Threads));
            }
        }

        private List<GroupForumPost> BuildThreadList(DataTable Table)
        {
            int b = Math.Min(Table.Rows.Count, 20);
            var list = new List<GroupForumPost>();
            for (int i = 0; i < b; i++)
            {
                DataRow row = Table.Rows[i];
                if (row != null) list.Add(new GroupForumPost(row));
            }
            return list.OrderByDescending(x => x.Pinned).ToList();
        }
    }
}