namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class VoucherRedeemOkComposer : ServerPacket
    {
        public VoucherRedeemOkComposer()
            : base(ServerPacketHeader.VoucherRedeemOkMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString("");//productName
            packet.WriteString("");//productDescription
        }
    }
}