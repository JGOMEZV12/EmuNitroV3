using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class WhisperComposer : ServerPacket
    {
        public int VirtualId { get; }
        public string Text { get; }
        public int Emotion { get; }
        public int Bubble { get; }

        public string Colour { get; }

        public WhisperComposer(int VirtualId, string Text, int Emotion, int Bubble, string Colour = "black")
            : base(ServerPacketHeader.WhisperMessageComposer)
        {
            this.VirtualId = VirtualId;
            this.Text = Text;
            this.Emotion = Emotion;
            this.Bubble = Bubble;
            this.Colour = Colour;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);
            packet.WriteString(Text);
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