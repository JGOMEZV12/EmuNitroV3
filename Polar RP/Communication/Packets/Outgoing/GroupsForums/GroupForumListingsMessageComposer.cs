using System;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumListingsMessageComposer : ServerPacket
    {
        public GroupForumListingsMessageComposer(int mode, int startIndex, List<Group> groups, GameClient session)
            : base(ServerPacketHeader.GroupForumListingsMessageComposer)
        {
            Compose(this, mode, startIndex, groups, session);
        }

        public void Compose(ServerPacket packet, int mode, int startIndex, List<Group> groups, GameClient session)
        {
            // ✅ Java: mode → totalSize → startIndex → count (max 20) → serializeForumData x count
            int count = Math.Min(groups.Count, 20);

            packet.WriteInteger(mode);
            packet.WriteInteger(groups.Count);  // total
            packet.WriteInteger(startIndex);
            packet.WriteInteger(count);

            int habboId = session.GetHabbo().Id;

            for (int i = startIndex; i < startIndex + count && i < groups.Count; i++)
            {
                Group g = groups[i];
                int totalThreads = 0;
                int totalComments = g.ForumMessagesCount;
                int newComments = 0;
                int lastPosterId = g.ForumLastPosterId;
                string lastPosterName = g.ForumLastPosterName;
                int lastPostTime = g.ForumLastPostTime;

                packet.WriteInteger(g.Id);
                packet.WriteString(g.Name);
                packet.WriteString(g.Description);
                packet.WriteString(g.Badge);
                packet.WriteInteger(totalThreads);
                packet.WriteInteger(0);              // rating
                packet.WriteInteger(totalComments);
                packet.WriteInteger(newComments);    // ✅ unread
                packet.WriteInteger(lastPosterId);   // ✅ lastComment threadId
                packet.WriteInteger(lastPosterId);   // ✅ lastComment userId
                packet.WriteString(lastPosterName);
                packet.WriteInteger(lastPostTime);
            }
        }
    }
}