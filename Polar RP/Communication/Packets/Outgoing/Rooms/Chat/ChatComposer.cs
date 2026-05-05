using System;
using System.Net.Sockets;

namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class ChatComposer : ServerPacket
    {
        public ChatComposer(int VirtualId, string Message, int Emotion, int bubble, string Colour = "black")
            : base(ServerPacketHeader.ChatMessageComposer)
        {
            base.WriteInteger(VirtualId);
            base.WriteString(Message);
            base.WriteInteger(Emotion);
            base.WriteInteger(bubble);
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