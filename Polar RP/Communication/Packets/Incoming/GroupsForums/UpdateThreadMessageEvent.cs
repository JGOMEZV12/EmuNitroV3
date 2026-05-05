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
    internal class UpdateThreadMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int GroupId = Packet.PopInt();
            int ThreadId = Packet.PopInt();
            bool Pin = Packet.PopBoolean();
            bool Lock = Packet.PopBoolean();

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
                    dbClient.SetQuery("UPDATE groups_forums_posts SET pinned = @pin, locked = @lock WHERE id = @tid");
                    dbClient.AddParameter("pin", Pin ? "1" : "0");
                    dbClient.AddParameter("lock", Lock ? "1" : "0");
                    dbClient.AddParameter("tid", ThreadId);
                    dbClient.RunQuery();
                }

                var Thread = new GroupForumPost(Row);

                if (Thread.Pinned != Pin)
                    Session.SendMessage(new RoomNotificationComposer(Pin ? "forums.thread.pinned" : "forums.thread.unpinned"));
                if (Thread.Locked != Lock)
                    Session.SendMessage(new RoomNotificationComposer(Lock ? "forums.thread.locked" : "forums.thread.unlocked"));

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

                int b = Math.Min(Table.Rows.Count, 20);
                var Threads = new List<GroupForumPost>();
                for (int i = 0; i < b; i++)
                {
                    DataRow row = Table.Rows[i];
                    if (row != null) Threads.Add(new GroupForumPost(row));
                }

                // El ordenamiento lo hace el composer internamente
                Session.SendMessage(new GroupForumThreadRootMessageComposer(Group, 0, Threads));
            }
        }
    }
}