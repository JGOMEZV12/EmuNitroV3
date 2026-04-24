using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Nux
{
    internal class NuxAlertComposer : ServerPacket
    {
        public string Message { get; }
        public NuxAlertComposer(string Message)
            : base(ServerPacketHeader.NuxAlertMessageComposer)
        {
            this.Message = Message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Message);
        }
    }
}
