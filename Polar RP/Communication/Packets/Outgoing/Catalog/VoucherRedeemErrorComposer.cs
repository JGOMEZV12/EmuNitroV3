namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class VoucherRedeemErrorComposer : ServerPacket
    {
        public int Type { get; }

        public VoucherRedeemErrorComposer(int Type)
            : base(ServerPacketHeader.VoucherRedeemErrorMessageComposer)
        {
            this.Type = Type;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Type.ToString());
        }
    }
}