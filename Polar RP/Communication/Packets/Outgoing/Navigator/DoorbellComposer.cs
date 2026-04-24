using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class DoorbellComposer : ServerPacket
    {
        public string Username { get; }

        public DoorbellComposer(string username)
            : base(ServerPacketHeader.DoorbellMessageComposer)
        {
            this.Username = username;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Username);
        }
    }
}
