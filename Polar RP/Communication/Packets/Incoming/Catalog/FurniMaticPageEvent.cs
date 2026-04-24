using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal class FurniMaticPageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() == null) return;

            Session.SendMessage(new FurniMaticNoRoomError());
        }
    }
}