using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Inventory.Trading
{
    internal class TradingConfirmedComposer : ServerPacket
    {
        public TradingConfirmedComposer(int UserId, bool Confirmed)
            : base(ServerPacketHeader.TradingConfirmedMessageComposer)
        {
            base.WriteInteger(UserId);
            base.WriteInteger(Confirmed ? 1 : 0);
        }
    }
}
