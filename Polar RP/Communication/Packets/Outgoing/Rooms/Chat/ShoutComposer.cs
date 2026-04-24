namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class ShoutComposer : ServerPacket
    {
        public int VirtualId { get; }
        public string Message { get; }
        public int Emotion { get; }
        public int Bubble { get; }

        public string Colour { get; }

        public ShoutComposer(int VirtualId, string Message, int Emotion, int Bubble, string Colour = "black")
            : base(ServerPacketHeader.ShoutMessageComposer)
        {
            this.VirtualId = VirtualId;
            this.Message = Message;
            this.Emotion = Emotion;
            this.Bubble = Bubble;
            this.Colour = Colour;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);
            packet.WriteString(Message);
            packet.WriteInteger(Emotion);
            packet.WriteInteger(Bubble);
            packet.WriteInteger(0);
            packet.WriteString(Colour);
            packet.WriteInteger(-1);
            packet.WriteString("");
            packet.WriteString("");
            packet.WriteString("");
            packet.WriteString("");
        }
    }
}