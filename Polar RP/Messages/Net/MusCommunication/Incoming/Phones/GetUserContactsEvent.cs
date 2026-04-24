using Polar.Messages.Net.MusCommunication.Outgoing.Phones;
using Polar.Net;
using System;
using System.Threading.Tasks;

namespace Polar.Messages.Net.MusCommunication.Incoming.Phones
{
    class GetUserContactsEvent : IMusPacketEvent
    {
        public async Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            string[] D = Packet.PacketData.Split('|');
            MUS.SendMessage(new SendUserContactsComposer(Convert.ToInt32(D[0]), Convert.ToInt32(D[1])));
        }
    }
}
