using System;
using System.Net.Sockets;

namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class ChatComposer : ServerPacket
    {
        public int VirtualId { get; }
        public string Message { get; }
        public int Emotion { get; }
        public int Bubble { get; }

        public string Colour { get; }

        public ChatComposer(int VirtualId, string Message, int Emotion, int Bubble, string Colour = "black")
            : base(ServerPacketHeader.ChatMessageComposer)
        {
            base.WriteInteger(VirtualId);
            base.WriteString(Message);
            base.WriteInteger(Emotion);
            base.WriteInteger(Bubble);
            base.WriteInteger(0);
            base.WriteString(Colour);
            base.WriteInteger(-1);
            base.WriteString("");
            base.WriteString("");
            base.WriteString("");
            base.WriteString("");
        }
    }
}