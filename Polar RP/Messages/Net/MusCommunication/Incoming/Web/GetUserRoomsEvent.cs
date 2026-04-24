using Polar.Messages.Net.MusCommunication.Outgoing.Web;
using Polar.Net;
using System;

namespace Polar.Messages.Net.MusCommunication.Incoming.Web
{
    class GetUserRoomsEvent : IMusPacketEvent
    {
        public async Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            MUS.SendMessage(new SendUserRoomsComposer());
        }
    }
}
