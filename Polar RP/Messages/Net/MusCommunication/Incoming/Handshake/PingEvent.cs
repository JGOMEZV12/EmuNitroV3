using Polar.Messages.Net.MusCommunication.Outgoing.Handshake;
using Polar.Net;
using System;

namespace Polar.Messages.Net.MusCommunication.Incoming.Handshake
{
    class PingEvent : IMusPacketEvent
    {
        public async Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            MUS.SendMessage(new PongComposer());
        }
    }
}
