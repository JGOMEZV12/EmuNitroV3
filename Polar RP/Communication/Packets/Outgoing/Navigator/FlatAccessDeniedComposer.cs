using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class FlatAccessDeniedComposer : ServerPacket
    {
        public string Username { get; }

        public FlatAccessDeniedComposer(string username)
            : base(ServerPacketHeader.FlatAccessDeniedMessageComposer)
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
