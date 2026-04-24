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
            string string1 = string.Empty, string2 = string.Empty, string3 = string.Empty, string4 = string.Empty;

            bool IsMember = false;
            bool IsAdmin = false;
            bool IsOwner = false;

            if (Session.GetHabbo().GetPermissions().HasRight("all_groups_member") || group.IsMember(Session.GetHabbo().Id))
                IsMember = true;
            if (Session.GetHabbo().GetPermissions().HasRight("all_groups_admin") || group.IsAdmin(Session.GetHabbo().Id))
                IsAdmin = true;
            if (Session.GetHabbo().GetPermissions().HasRight("all_groups_owner") || group.CreatorId == Session.GetHabbo().Id)
                IsOwner = true;

            packet.WriteInteger(group.Id);
            packet.WriteString(group.Name);
            packet.WriteString(group.Description);
            packet.WriteString(group.Badge);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(group.ForumMessagesCount);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(group.ForumLastPosterId);
            packet.WriteString(group.ForumLastPosterName);
            packet.WriteInteger(group.ForumLastPostTime);
            packet.WriteInteger(group.WhoCanRead);
            packet.WriteInteger(group.WhoCanPost);
            packet.WriteInteger(group.WhoCanThread);
            packet.WriteInteger(group.WhoCanMod);

            if (group.WhoCanRead == 1 && !IsMember)
                string1 = "not_member";
            if (group.WhoCanRead == 2 && !IsAdmin)
                string1 = "not_admin";
            if (group.WhoCanRead == 3 && !IsOwner)
                string1 = "not_owner";

            if (group.WhoCanPost == 1 && !IsMember)
                string2 = "not_member";
            if (group.WhoCanPost == 2 && !IsAdmin)
                string2 = "not_admin";
            if (group.WhoCanPost == 3 && !IsOwner)
                string2 = "not_owner";

            if (group.WhoCanThread == 1 && !IsMember)
                string3 = "not_member";
            if (group.WhoCanThread == 2 && !IsAdmin)
                string3 = "not_admin";
            if (group.WhoCanThread == 3 && !IsOwner)
                string3 = "not_owner";

            if (group.WhoCanMod == 2 && !IsAdmin)
                string4 = "not_admin";
            if (group.WhoCanMod == 3 && !IsOwner)
                string4 = "not_owner";

            packet.WriteString(string1);
            packet.WriteString(string2);
            packet.WriteString(string3);
            packet.WriteString(string4);
            packet.WriteString(string.Empty);
            packet.WriteBoolean(Session.GetHabbo().Id == group.CreatorId);
            packet.WriteBoolean(true);
        }
    }
}