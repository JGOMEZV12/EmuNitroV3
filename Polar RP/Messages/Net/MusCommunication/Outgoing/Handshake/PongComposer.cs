
namespace Polar.Messages.Net.MusCommunication.Outgoing.Handshake
{
    class PongComposer : MusPacketEvent
    {
        public PongComposer()
        {
            // Incoming Information to send to Client
            PacketName = "event_pong";
            PacketData = "";
        }
    }
}
