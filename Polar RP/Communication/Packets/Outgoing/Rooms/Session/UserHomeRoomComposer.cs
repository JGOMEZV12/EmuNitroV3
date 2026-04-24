namespace Polar.Communication.Packets.Outgoing.Rooms.Session
{
    class UserHomeRoomComposer : ServerPacket
    {

        public UserHomeRoomComposer(int homeRoom, int roomToEnter)
            : base(ServerPacketHeader.UserHomeRoomComposer)
        {
            base.WriteInteger(homeRoom);
            base.WriteInteger(roomToEnter);
        }
    }
}
