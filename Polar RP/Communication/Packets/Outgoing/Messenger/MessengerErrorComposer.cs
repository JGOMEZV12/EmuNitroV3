using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class MessengerErrorComposer : ServerPacket
    {
        public int ErrorCode1 { get; }
        public int ErrorCode2 { get; }

        public MessengerErrorComposer(int ErrorCode1, int ErrorCode2)
            : base(ServerPacketHeader.MessengerErrorMessageComposer)
        {
            this.ErrorCode1 = ErrorCode1;
            this.ErrorCode2 = ErrorCode2;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ErrorCode1);
            packet.WriteInteger(ErrorCode2);
        }
    }
}
