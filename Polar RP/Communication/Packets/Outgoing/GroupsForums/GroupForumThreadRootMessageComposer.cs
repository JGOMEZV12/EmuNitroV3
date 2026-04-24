using System;
using System.Collections.Generic;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumThreadRootMessageComposer : ServerPacket
    {
        public Group Group { get; }
        public int Type { get; }
        public int StartIndex { get; }
        public int b { get; }
        public List<GroupForumPost> Threads { get; }
        public GameClient Session { get; }
        public GroupForumThreadRootMessageComposer(Group Group, int Type, int StartIndex, int b, List<GroupForumPost> Threads, GameClient Session)
            : base(ServerPacketHeader.GroupForumThreadRootMessageComposer)
        {
            this.Group = Group;
            this.Type = Type;
            this.StartIndex = StartIndex;
            this.b = b;
            this.Threads = Threads;
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (Type == 1)
            {
                packet.WriteInteger(Group.Id);
                packet.WriteInteger(0);
                packet.WriteInteger(0);
            }
            if (Type == 2)
            {
                packet.WriteInteger(Group.Id);
                packet.WriteInteger(StartIndex);
                packet.WriteInteger(b);

                foreach (GroupForumPost Thread in Threads)
                {
                    packet.WriteInteger(Thread.Id);
                    packet.WriteInteger(Thread.PosterId);
                    packet.WriteString(Thread.PosterName);
                    packet.WriteString(Thread.Subject);
                    packet.WriteBoolean(Thread.Pinned);
                    packet.WriteBoolean(Thread.Locked);
                    packet.WriteInteger((Convert.ToInt32(PolarEnvironment.GetUnixTimestamp()) - Thread.Timestamp));
                    packet.WriteInteger(Thread.MessageCount + 1);
                    packet.WriteInteger(0);
                    packet.WriteInteger(0);
                    packet.WriteInteger(0);
                    packet.WriteString(Group.ForumLastPosterName);
                    packet.WriteInteger((Convert.ToInt32(PolarEnvironment.GetUnixTimestamp()) - Thread.Timestamp));
                    packet.WriteByte((Thread.Hidden) ? 10 : 1);
                    packet.WriteInteger(0);
                    if (Thread.Hider != 0)
                        packet.WriteString(PolarEnvironment.GetHabboById(Thread.Hider).Username);
                    else
                        packet.WriteString("");
                    packet.WriteInteger(0);
                }
            }
        }
    }
}