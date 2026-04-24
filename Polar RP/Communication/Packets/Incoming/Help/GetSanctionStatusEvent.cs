using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Help;

namespace Polar.Communication.Packets.Incoming.Help
{
    internal class GetSanctionStatusEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new SanctionStatusComposer());
        }
    }
}
