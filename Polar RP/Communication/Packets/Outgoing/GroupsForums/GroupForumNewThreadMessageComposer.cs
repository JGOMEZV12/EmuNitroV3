using System;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumNewThreadMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public Group Group { get; }
        public int groupid { get; }
        public int threadid { get; }
        public string subject { get; }
        public string content { get; }
        public int timestamp { get; }
        public GroupForumNewThreadMessageComposer(GameClient session, int groupId, int threadId, string subject, string content, int timeStamp)
            : base(ServerPacketHeader.GroupForumNewThreadMessageComposer)
        {
            this.Session = session;
            this.groupid = groupId;
            this.threadid = threadId;
            this.subject = subject;
            this.content = content;
            this.timestamp = timeStamp;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(groupid);
            packet.WriteInteger(threadid);
            packet.WriteInteger(Session.GetHabbo().Id);
            packet.WriteString(subject);
            packet.WriteString(content);
            packet.WriteBoolean(false);
            packet.WriteBoolean(false);
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - timestamp));
            packet.WriteInteger(1);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteString("");
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - timestamp));
            packet.WriteByte(1);
            packet.WriteInteger(1);
            packet.WriteString("");
            packet.WriteInteger(42);//useless
        }
    }
}