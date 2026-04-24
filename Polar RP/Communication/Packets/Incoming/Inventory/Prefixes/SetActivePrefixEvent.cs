using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Inventory.Prefixes;

namespace Polar.Communication.Packets.Incoming.Inventory.Prefixes
{
    public class SetActivePrefixEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            int prefixId = packet.PopInt();

            var prefixesComponent = session.GetHabbo().GetInventoryComponent().GetPrefixesComponent();

            if (prefixId == 0)
            {
                prefixesComponent.DeactivateAll();
                session.SendMessage(new ActivePrefixUpdatedComposer(null));
                return;
            }

            UserPrefix prefix = prefixesComponent.GetPrefix(prefixId);
            if (prefix == null) return;

            prefixesComponent.SetActive(prefixId);
            session.SendMessage(new ActivePrefixUpdatedComposer(prefix));
        }
    }
}