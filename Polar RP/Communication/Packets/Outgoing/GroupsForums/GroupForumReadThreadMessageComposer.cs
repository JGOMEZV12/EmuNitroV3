using System;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumReadThreadMessageComposer : ServerPacket
    {
        public GroupForumReadThreadMessageComposer(int groupId, int threadId, int startIndex, List<GroupForumPost> posts)
            : base(ServerPacketHeader.GroupForumReadThreadMessageComposer)
        {
            Compose(this, groupId, threadId, startIndex, posts);
        }

        public void Compose(ServerPacket packet, int groupId, int threadId, int startIndex, List<GroupForumPost> posts)
        {
            // ✅ Java GuildForumCommentsComposer: guildId → threadId → startIndex → count → comments
            packet.WriteInteger(groupId);
            packet.WriteInteger(threadId);
            packet.WriteInteger(startIndex);
            packet.WriteInteger(posts.Count);

            int now = Convert.ToInt32(PolarEnvironment.GetUnixTimestamp());

            foreach (GroupForumPost post in posts)
            {
                // ✅ Java: comment.serialize() — sin índices manuales, sin ForumPosts extra
                packet.WriteInteger(post.Id);
                packet.WriteInteger(post.PosterId);
                packet.WriteString(post.PosterName);
                packet.WriteString(post.PosterLook);
                packet.WriteInteger(now - post.Timestamp);
                packet.WriteString(post.PostContent);
                packet.WriteByte(post.Hidden ? (byte)10 : (byte)0);
                packet.WriteInteger(0);
                packet.WriteString(post.Hider != 0
                    ? (PolarEnvironment.GetHabboById(post.Hider)?.Username ?? "")
                    : "");
                packet.WriteInteger(0);
            }
        }
    }
}