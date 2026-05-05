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
    internal class DeleteGroupPostMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int groupId = Packet.PopInt();
            int parentId = Packet.PopInt();
            int index = Packet.PopInt() + 1;
            int StateToSet = Packet.PopInt();

            Group group = GroupManager.GetJob(groupId);
            if (group == null || !group.ForumEnabled) return;

            bool IsAdmin = group.IsAdmin(Session.GetHabbo().Id)
                        || Session.GetHabbo().GetPermissions().HasRight("corporation_rights")
                        || group.CreatorId == Session.GetHabbo().Id
                        || Session.GetHabbo().GetPermissions().HasRight("roleplay_corp_manager");

            if (!IsAdmin) return;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE parent_id = @pid ORDER BY id");
                dbClient.AddParameter("pid", parentId);
                DataTable Table = dbClient.getTable();

                int t = 0;
                foreach (DataRow Row in Table.Rows)
                {
                    t++;
                    if (t == index)
                    {
                        string state = (StateToSet == 20 || StateToSet == 10) ? "1" : "0";
                        dbClient.SetQuery("UPDATE groups_forums_posts SET hidden = @hid WHERE id = @id");
                        dbClient.AddParameter("id", Convert.ToInt32(Row["id"]));
                        dbClient.AddParameter("hid", state);
                        dbClient.RunQuery();
                        break;
                    }
                }

                Session.SendMessage(new RoomNotificationComposer(
                    (StateToSet == 20 || StateToSet == 10) ? "forums.message.hidden" : "forums.message.restored"));

                dbClient.SetQuery("SELECT * FROM groups_forums_posts WHERE group_id = @groupid AND parent_id = @threadid OR id = @threadid ORDER BY timestamp ASC");
                dbClient.AddParameter("groupid", groupId);
                dbClient.AddParameter("threadid", parentId);
                DataTable Table2 = dbClient.getTable();
                if (Table2 == null) return;

                int b = Math.Min(Table2.Rows.Count, 20);
                var posts = new List<GroupForumPost>();
                for (int i = 0; i < b; i++)
                {
                    DataRow row = Table2.Rows[i];
                    if (row == null) continue;
                    var post = new GroupForumPost(row);
                    if (post.ParentId == 0 && post.Hidden) return;
                    posts.Add(post);
                }

                // ✅ Nuevo constructor: (groupId, threadId, startIndex, posts)
                Session.SendMessage(new GroupForumReadThreadMessageComposer(groupId, parentId, 0, posts));
            }
        }
    }
}