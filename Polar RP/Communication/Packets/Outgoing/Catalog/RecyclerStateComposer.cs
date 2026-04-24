namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class RecyclerStateComposer : ServerPacket
    {
        public int itemId { get; }
        public RecyclerStateComposer(int ItemId = 0)
            : base(ServerPacketHeader.RecyclerStateComposer)
        {
            this.itemId = ItemId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);
            packet.WriteInteger(itemId);
        }
    }
}