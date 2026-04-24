using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Misc
{
    internal class InternalLinkComposer : ServerPacket
    {
        public string Link { get; }
        public InternalLinkComposer(string link)
            : base(ServerPacketHeader.InternalLinkComposer)
        {
            this.Link = link;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Link);
        }
    }
}
