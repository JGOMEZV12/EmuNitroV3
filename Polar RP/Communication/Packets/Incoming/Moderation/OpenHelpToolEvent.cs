using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.Communication.Packets.Incoming.Moderation
{
    internal class OpenHelpToolEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new OpenHelpToolComposer());
        }
    }
}
