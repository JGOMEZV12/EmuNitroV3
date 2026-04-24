namespace Polar.Communication.Packets.Outgoing.Handshake
{
    public class SecretKeyComposer : ServerPacket
    {
 
        public SecretKeyComposer(string publicKey)
            : base(ServerPacketHeader.SecretKeyMessageComposer)
        {
           
            base.WriteString(publicKey);
        }
    }
}