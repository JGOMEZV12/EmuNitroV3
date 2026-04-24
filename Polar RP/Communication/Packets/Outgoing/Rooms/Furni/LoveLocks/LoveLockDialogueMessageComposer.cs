namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.LoveLocks
{
    internal class LoveLockDialogueMessageComposer : ServerPacket
    {
        public int ItemId { get; }

        public LoveLockDialogueMessageComposer(int ItemId)
            : base(ServerPacketHeader.LoveLockDialogueMessageComposer)
        {
            this.ItemId = ItemId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ItemId);
            packet.WriteBoolean(true);
        }
    }
}
