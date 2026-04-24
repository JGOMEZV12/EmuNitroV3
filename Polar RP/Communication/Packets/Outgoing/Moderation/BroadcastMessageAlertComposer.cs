using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Moderation
{
    internal class BroadcastMessageAlertComposer : ServerPacket
    {
        public string Message { get; }
        public string Url { get; }
        public BroadcastMessageAlertComposer(string Message, string URL = "")
            : base(ServerPacketHeader.BroadcastMessageAlertMessageComposer)
        {
            this.Message = Message;
            this.Url = URL;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Message);
            packet.WriteString(Url);
        }
    }
}

