using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Prefixes;

namespace Polar.Communication.Packets.Incoming.Inventory.Prefixes
{
    public class RequestUserPrefixesEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            session.SendMessage(new UserPrefixesComposer(session.GetHabbo()));
        }
    }
}