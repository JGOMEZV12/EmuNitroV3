using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorLiftedRoomsComposer : ServerPacket
    {
        public NavigatorLiftedRoomsComposer()
            : base(ServerPacketHeader.NavigatorLiftedRoomsMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);//Count
            {
                packet.WriteInteger(1);//Flat Id
                packet.WriteInteger(0);//Unknown
                packet.WriteString("");//Image
                packet.WriteString("Caption");//Caption.
            }
        }
    }
}
