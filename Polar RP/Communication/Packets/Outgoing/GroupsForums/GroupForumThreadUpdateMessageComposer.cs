using System;
using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumThreadUpdateMessageComposer : ServerPacket
    {
        public GroupForumThreadUpdateMessageComposer(Group group, GroupForumPost thread)
            : base(ServerPacketHeader.GroupForumThreadUpdateMessageComposer)
        {
            Compose(this, group, thread);
        }

        public void Compose(ServerPacket packet, Group group, GroupForumPost thread)
        {
            // ✅ Java ThreadUpdatedMessageComposer: guildId → thread.serialize()
            int now = Convert.ToInt32(PolarEnvironment.GetUnixTimestamp());

            packet.WriteInteger(group.Id);

            // thread.serialize()
            packet.WriteInteger(thread.Id);
            packet.WriteInteger(thread.PosterId);
            packet.WriteString(thread.PosterName);
            packet.WriteString(thread.Subject);
            packet.WriteBoolean(thread.Pinned);
            packet.WriteBoolean(thread.Locked);
            packet.WriteInteger(now - thread.Timestamp);
            packet.WriteInteger(thread.MessageCount + 1);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteString(group.ForumLastPosterName);
            packet.WriteInteger(now - thread.Timestamp);
            packet.WriteByte(thread.Hidden ? (byte)10 : (byte)1);
            packet.WriteInteger(0);
            // ✅ null-check: Hider puede ser 0
            packet.WriteString(thread.Hider != 0
                ? (PolarEnvironment.GetHabboById(thread.Hider)?.Username ?? "")
                : "");
            packet.WriteInteger(0);
        }
    }
}