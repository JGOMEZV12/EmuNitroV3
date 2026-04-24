using System.Linq;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;


namespace Polar.Communication.Packets.Outgoing.Inventory.Trading
{
    internal class TradingUpdateComposer : ServerPacket
    {
        public TradingUpdateComposer(Trade trade)
            : base(ServerPacketHeader.TradingUpdateMessageComposer)
        {
        }
    }
}