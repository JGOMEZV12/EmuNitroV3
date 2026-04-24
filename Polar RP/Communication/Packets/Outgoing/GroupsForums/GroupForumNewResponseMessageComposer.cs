using System;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumNewResponseMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public Group Group { get; }
        public int groupid { get; }
        public int threadid { get; }
        public string content { get; }
        public int timestamp { get; }
        public GroupForumNewResponseMessageComposer(GameClient session, Group group, int groupId, int threadId, string content, int timeStamp)
            : base(ServerPacketHeader.GroupForumNewResponseMessageComposer)
        {
            this.Session = session;
            this.Group = group;
            this.groupid = groupId;
            this.threadid = threadId;
            this.content = content;
            this.timestamp = timeStamp;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(groupid);
            packet.WriteInteger(threadid);
            packet.WriteInteger(Group.ForumMessagesCount);
            packet.WriteInteger(0);
            packet.WriteInteger(Session.GetHabbo().Id);
            packet.WriteString(Session.GetHabbo().Username);
            packet.WriteString(Session.GetHabbo().Look);
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - timestamp));
            packet.WriteString(content);
            packet.WriteByte(0);
            packet.WriteInteger(0);
            packet.WriteString("");
            packet.WriteInteger(0);
        }
    }
}