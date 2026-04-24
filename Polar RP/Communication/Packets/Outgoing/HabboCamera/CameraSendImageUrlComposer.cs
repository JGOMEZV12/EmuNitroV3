using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.HabboCamera
{
    internal class CameraSendImageUrlComposer : ServerPacket
    {
        public string Url { get; }
        public CameraSendImageUrlComposer(string url)
            : base(ServerPacketHeader.CameraSendImageUrlMessageComposer)
        {
            this.Url = url;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Url);
        }
    }
}
