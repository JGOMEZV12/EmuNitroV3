using System;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumReadThreadMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public int GroupId { get; }
        public int ThreadId { get; }
        public int StartIndex { get; }
        public int b { get; }
        public int indx;
        public List<GroupForumPost> posts { get; }
        public GroupForumReadThreadMessageComposer(GameClient Session, int GroupId, int ThreadId, int StartIndex, int b, int indx, List<GroupForumPost> posts)
            : base(ServerPacketHeader.GroupForumReadThreadMessageComposer)
        {
            this.Session = Session;
            this.GroupId = GroupId;
            this.ThreadId = ThreadId;
            this.StartIndex = StartIndex;
            this.b = b;
            this.indx = indx;
            this.posts = posts;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GroupId);
            packet.WriteInteger(ThreadId);
            packet.WriteInteger(StartIndex);
            packet.WriteInteger(b);

            foreach (GroupForumPost Post in posts)
            {
                packet.WriteInteger(indx++ - 1);
                packet.WriteInteger(indx - 1);
                packet.WriteInteger(Post.PosterId);
                packet.WriteString(Post.PosterName);
                packet.WriteString(Post.PosterLook);
                packet.WriteInteger(Convert.ToInt32(PolarEnvironment.GetUnixTimestamp()) - Post.Timestamp);
                packet.WriteString(Post.PostContent);
                if (Post.Hidden)
                    packet.WriteByte(10);
                else
                    packet.WriteByte(0);
                packet.WriteInteger(0);
                if (Post.Hider != 0)
                    packet.WriteString(PolarEnvironment.GetHabboById(Post.Hider).Username);
                else
                    packet.WriteString("");
                packet.WriteInteger(0);
                packet.WriteInteger(PolarEnvironment.GetHabboById(Post.PosterId).GetStats().ForumPosts);
            }
        }
    }
}