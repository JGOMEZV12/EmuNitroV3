namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class UserTypingComposer : ServerPacket
    {
        public int VirtualId { get; }
        public bool Typing { get; }

        public UserTypingComposer(int VirtualId, bool Typing)
            : base(ServerPacketHeader.UserTypingMessageComposer)
        {
            this.VirtualId = VirtualId;
            this.Typing = Typing;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);
            packet.WriteInteger(Typing ? 1 : 0);
        }
    }
}