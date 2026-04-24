using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class FollowFriendFailedComposer : ServerPacket
    {
        public int ErrorCode { get; }

        public FollowFriendFailedComposer(int ErrorCode)
            : base(ServerPacketHeader.FollowFriendFailedMessageComposer)
        {
            this.ErrorCode = ErrorCode;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ErrorCode);
        }
    }
}
