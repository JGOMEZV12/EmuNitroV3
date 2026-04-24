using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Inventory.Prefixes;

namespace Polar.Communication.Packets.Incoming.Inventory.Prefixes
{
    public class DeletePrefixEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            int prefixId = packet.PopInt();

            UserPrefix prefix = session.GetHabbo().GetInventoryComponent()
                .GetPrefixesComponent().GetPrefix(prefixId);

            if (prefix == null) return;

            session.GetHabbo().GetInventoryComponent()
                .GetPrefixesComponent().RemovePrefix(prefix);

            prefix.SetNeedsDelete(true);
            Task.Run(() => prefix.Save());

            session.SendMessage(new UserPrefixesComposer(session.GetHabbo()));
        }
    }
}