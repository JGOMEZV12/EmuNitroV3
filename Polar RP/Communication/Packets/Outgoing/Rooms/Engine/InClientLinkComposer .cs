using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class InClientLinkComposer : ServerPacket
    {
        public InClientLinkComposer(string link)
            : base(ServerPacketHeader.InClientLinkComposer)
        {

            base.WriteString(link);
        }
    }
}
