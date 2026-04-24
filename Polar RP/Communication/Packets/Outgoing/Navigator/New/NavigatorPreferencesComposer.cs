using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorPreferencesComposer : ServerPacket
    {
        public NavigatorPreferencesComposer()
            : base(ServerPacketHeader.NavigatorPreferencesMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(68);
			packet.WriteInteger(42);
            packet.WriteInteger(425);//Width
            packet.WriteInteger(592);//Height
            packet.WriteBoolean(false);
			packet.WriteInteger(0);
        }
    }
}

