namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class ShoutComposer : ServerPacket
    {
        public ShoutComposer(int VirtualId, string Message, int Emotion, int Bubble, string Colour = "black")
            : base(ServerPacketHeader.ShoutMessageComposer)
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