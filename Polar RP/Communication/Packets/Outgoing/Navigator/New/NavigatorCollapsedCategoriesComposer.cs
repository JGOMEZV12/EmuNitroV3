using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorCollapsedCategoriesComposer : ServerPacket
    {
        public NavigatorCollapsedCategoriesComposer()
            : base(ServerPacketHeader.NavigatorCollapsedCategoriesMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(0);
        }
    }
}
