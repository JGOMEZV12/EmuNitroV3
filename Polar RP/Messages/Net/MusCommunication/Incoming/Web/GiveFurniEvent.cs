using Polar.Messages.Net.MusCommunication.Outgoing.Web;
using Polar.Net;
using System;

namespace Polar.Messages.Net.MusCommunication.Incoming.Web
{
    class GiveFurniEvent : IMusPacketEvent
    {
        public Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            return Task.CompletedTask;
        }
    }
}
