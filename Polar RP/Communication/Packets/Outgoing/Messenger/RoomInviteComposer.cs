using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class RoomInviteComposer : ServerPacket
    {
        public int SenderId { get; }
        public string Text { get; }

        public RoomInviteComposer(int SenderId, string Text)
            : base(ServerPacketHeader.RoomInviteMessageComposer)
        {
            this.SenderId = SenderId;
            this.Text = Text;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(SenderId);
            packet.WriteString(Text);
        }
    }
}
