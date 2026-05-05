using System;
using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumThreadRootMessageComposer : ServerPacket
    {
        public GroupForumThreadRootMessageComposer(Group group, int startIndex, List<GroupForumPost> threads)
            : base(ServerPacketHeader.GroupForumThreadRootMessageComposer)
        {
            Compose(this, group, startIndex, threads);
        }

        public void Compose(ServerPacket packet, Group group, int startIndex, List<GroupForumPost> threads)
        {
            // ✅ Java GuildForumThreadsComposer: ordena pinned primero, luego updatedAt desc, límite 20
            var sorted = threads
                .OrderByDescending(t => t.Pinned)
                .ThenByDescending(t => t.Timestamp)
                .ToList();

            int count = Math.Min(sorted.Count, 20);
            int now = Convert.ToInt32(PolarEnvironment.GetUnixTimestamp());

            packet.WriteInteger(group.Id);
            packet.WriteInteger(startIndex);
            packet.WriteInteger(count);

            for (int i = startIndex; i < startIndex + count && i < sorted.Count; i++)
            {
                GroupForumPost thread = sorted[i];

                // ✅ Java: thread.serialize()
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
                packet.WriteString(thread.Hider != 0
                    ? (PolarEnvironment.GetHabboById(thread.Hider)?.Username ?? "")
                    : "");
                packet.WriteInteger(0);
            }
        }
    }
}