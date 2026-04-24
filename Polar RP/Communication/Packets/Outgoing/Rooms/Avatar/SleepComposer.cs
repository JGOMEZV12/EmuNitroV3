using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Avatar
{
    public class SleepComposer : ServerPacket
    {
        public RoomUser User { get; }
        public bool IsSleeping { get; }
        public SleepComposer(RoomUser user, bool IsSleeping)
            : base(ServerPacketHeader.SleepMessageComposer)
        {
            this.User = user;
            this.IsSleeping = IsSleeping;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {

            packet.WriteInteger(User.VirtualId);
            packet.WriteBoolean(IsSleeping);
        }
    }
}