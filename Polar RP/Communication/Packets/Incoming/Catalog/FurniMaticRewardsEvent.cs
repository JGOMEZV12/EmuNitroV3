using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal class FurniMaticRewardsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            
            Session.SendMessage(new FurniMaticRewardsComposer());
        }
    }
}

