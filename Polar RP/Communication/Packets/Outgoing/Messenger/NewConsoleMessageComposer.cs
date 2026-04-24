using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class NewConsoleMessageComposer : ServerPacket
    {
        public int Sender { get; }
        public string Message { get; }
        public int Time { get; }

        public NewConsoleMessageComposer(int Sender, string Message, int Time = 0)
            : base(ServerPacketHeader.NewConsoleMessageMessageComposer)
        {
            this.Sender = Sender;
            this.Message = Message;
            this.Time = Time;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Sender);
            packet.WriteString(Message);
            packet.WriteInteger(Time);
        }
    }
    internal class FuckingConsoleMessageComposer : ServerPacket
    {
        public int Sender { get; }
        public string Message { get; }
        public string Data { get; }
        public FuckingConsoleMessageComposer(int sender, string message, string data)
            : base(ServerPacketHeader.NewConsoleMessageMessageComposer)
        {
            this.Sender = sender;
            this.Message = message;
            this.Data = data;
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Sender);
            packet.WriteString(Message);
            packet.WriteInteger(0);
            packet.WriteString(Data);
        }
    }
}
