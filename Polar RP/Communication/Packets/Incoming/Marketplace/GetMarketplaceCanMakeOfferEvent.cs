using Polar.Communication.Packets.Outgoing.Marketplace;

namespace Polar.Communication.Packets.Incoming.Marketplace
{
    internal class GetMarketplaceCanMakeOfferEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new MarketplaceCanMakeOfferResultComposer((Session.GetHabbo().TradingLockExpiry > 0 ? 6 : 1)));
        }
    }
}