using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorSettingsComposer : ServerPacket
    {

        public NavigatorSettingsComposer(int HomeRoomId)
            : base(ServerPacketHeader.NavigatorSettingsMessageComposer)
        {
            base.WriteInteger(HomeRoomId);
            base.WriteInteger(HomeRoomId);
        }

    }
}
