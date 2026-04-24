using System;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupForumThreadUpdateMessageComposer : ServerPacket
    {
        public Group Group { get; }
        public GroupForumPost Thread { get; }
        public bool Pin { get; }
        public bool Lock { get; }
        public GroupForumThreadUpdateMessageComposer(Group Group, GroupForumPost Thread, bool Pin, bool Lock)
            : base(ServerPacketHeader.GroupForumThreadUpdateMessageComposer)
        {
            this.Group = Group;
            this.Thread = Thread;
            this.Pin = Pin;
            this.Lock = Lock;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Group.Id);
            packet.WriteInteger(Thread.Id);
            packet.WriteInteger(Thread.PosterId);
            packet.WriteString(Thread.PosterName);
            packet.WriteString(Thread.Subject);
            packet.WriteBoolean(Pin);
            packet.WriteBoolean(Lock);
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - Thread.Timestamp));
            packet.WriteInteger(Thread.MessageCount + 1);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(1);
            packet.WriteString("");
            packet.WriteInteger(((int)PolarEnvironment.GetUnixTimestamp() - Thread.Timestamp));
            packet.WriteByte((Thread.Hidden) ? 10 : 1);
            packet.WriteInteger(1);
            packet.WriteString(PolarEnvironment.GetHabboById(Thread.Hider).Username);
            packet.WriteInteger(0);
        }
    }
}