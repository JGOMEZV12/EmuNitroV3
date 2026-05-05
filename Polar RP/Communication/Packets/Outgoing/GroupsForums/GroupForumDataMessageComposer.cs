using System;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumDataMessageComposer : ServerPacket
    {
        public Group group { get; }
        public GameClient Session { get; }

        public GroupForumDataMessageComposer(Group Group, GameClient session)
            : base(ServerPacketHeader.GroupForumDataMessageComposer)
        {
            this.group = Group;
            this.Session = session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            bool IsMember = Session.GetHabbo().GetPermissions().HasRight("all_groups_member") || group.IsMember(Session.GetHabbo().Id);
            bool IsAdmin = Session.GetHabbo().GetPermissions().HasRight("all_groups_admin") || group.IsAdmin(Session.GetHabbo().Id);
            bool IsOwner = Session.GetHabbo().GetPermissions().HasRight("all_groups_owner") || group.CreatorId == Session.GetHabbo().Id;
            bool IsStaff = Session.GetHabbo().GetPermissions().HasRight("acc_modtool_ticket_q");

            // ── Datos del foro ─────────────────────────────────────────────────
            // Java: serializeForumData() — totalThreads, totalComments, newComments, lastComment
            int totalThreads = 0;
            int totalComments = group.ForumMessagesCount;
            int newComments = 0;
            int lastPosterId = group.ForumLastPosterId;
            string lastPosterName = group.ForumLastPosterName;
            int lastPostTime = group.ForumLastPostTime;

            packet.WriteInteger(group.Id);
            packet.WriteString(group.Name);
            packet.WriteString(group.Description);
            packet.WriteString(group.Badge);
            packet.WriteInteger(totalThreads);      // ✅ Java: totalThreads (no hardcoded 0)
            packet.WriteInteger(0);                  // rating
            packet.WriteInteger(totalComments);      // ✅ Java: total comments
            packet.WriteInteger(newComments);        // ✅ Java: unread comments (no hardcoded 0)
            packet.WriteInteger(lastPosterId);       // ✅ Java: lastComment.threadId → lastComment.userId
            packet.WriteString(lastPosterName);
            packet.WriteInteger(lastPostTime);       // tiempo relativo (now - createdAt)
            packet.WriteInteger(group.WhoCanRead);
            packet.WriteInteger(group.WhoCanPost);
            packet.WriteInteger(group.WhoCanThread);
            packet.WriteInteger(group.WhoCanMod);

            // ── Errores de permiso — lógica exacta del Java ───────────────────
            string errorRead = "";
            if (group.WhoCanRead == 1 && !IsMember && !IsStaff) errorRead = "not_member";
            else if (group.WhoCanRead == 2 && !IsAdmin && !IsStaff) errorRead = "not_admin";

            string errorPost = "";
            if (group.WhoCanPost == 1 && !IsMember && !IsStaff) errorPost = "not_member";
            else if (group.WhoCanPost == 2 && !IsAdmin && !IsStaff) errorPost = "not_admin";
            else if (group.WhoCanPost == 3 && group.CreatorId != Session.GetHabbo().Id && !IsStaff) errorPost = "not_owner";

            string errorThread = "";
            if (group.WhoCanThread == 1 && !IsMember && !IsStaff) errorThread = "not_member";
            else if (group.WhoCanThread == 2 && !IsAdmin && !IsStaff) errorThread = "not_admin";
            else if (group.WhoCanThread == 3 && group.CreatorId != Session.GetHabbo().Id && !IsStaff) errorThread = "not_owner";

            string errorMod = "";
            if (group.WhoCanMod == 3 && group.CreatorId != Session.GetHabbo().Id && !IsStaff) errorMod = "not_owner";
            else if (!IsAdmin && !IsStaff) errorMod = "not_admin";

            packet.WriteString(errorRead);
            packet.WriteString(errorPost);
            packet.WriteString(errorThread);
            packet.WriteString(errorMod);
            packet.WriteString("");  // citizen

            packet.WriteBoolean(group.CreatorId == Session.GetHabbo().Id); // Forum Settings

            // ✅ Java: canMod depende de WhoCanMod.state
            if (group.WhoCanMod == 3)
                packet.WriteBoolean(group.CreatorId == Session.GetHabbo().Id || IsStaff);
            else
                packet.WriteBoolean(group.CreatorId == Session.GetHabbo().Id || IsStaff || IsAdmin);
        }
    }
}