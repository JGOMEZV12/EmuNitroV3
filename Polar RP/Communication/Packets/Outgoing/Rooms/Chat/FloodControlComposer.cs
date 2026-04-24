namespace Polar.Communication.Packets.Outgoing.Rooms.Chat
{
    public class FloodControlComposer : ServerPacket
    {
        public int FloodTime { get; }

        public FloodControlComposer(int floodTime)
            : base(ServerPacketHeader.FloodControlMessageComposer)
        {
            this.FloodTime = floodTime;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(FloodTime);
        }
    }
}