using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Handshake
{
    internal class VideoOffersRewardsComposer : ServerPacket
    {
        public int OfferId { get; }
        public string Type { get; }
        public string Message { get; }

        public VideoOffersRewardsComposer(int Id, string Type, string Message)
            : base(ServerPacketHeader.VideoOffersRewardsMessageComposer)
        {
            this.OfferId = Id;
            this.Type = Type;
            this.Message = Message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Type);
            packet.WriteInteger(OfferId);
            packet.WriteString(Message);
            packet.WriteString("");
        }
    }
}

