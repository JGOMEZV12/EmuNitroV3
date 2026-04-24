namespace Polar.Communication.Packets.Outgoing.Navigator
{
    public class UpdateFavouriteRoomComposer : ServerPacket
    {
        public int RoomId { get; }
        public bool Added { get; }

        public UpdateFavouriteRoomComposer(int roomId, bool added)
            : base(ServerPacketHeader.UpdateFavouriteRoomMessageComposer)
        {
            this.RoomId = roomId;
            this.Added = added;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
            packet.WriteBoolean(Added);
        }
    }
}