using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class WhisperComposer : ServerPacket
    {
        public WhisperComposer(int VirtualId, string Text, int Emotion, int Bubble, string Colour = "black")
            : base(ServerPacketHeader.WhisperMessageComposer)
        {
            base.WriteInteger(VirtualId);
            base.WriteString(Text);
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