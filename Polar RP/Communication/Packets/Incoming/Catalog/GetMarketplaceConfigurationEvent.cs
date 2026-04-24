using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Incoming;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    public class GetMarketplaceConfigurationEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new MarketplaceConfigurationComposer());
        }
    }
}