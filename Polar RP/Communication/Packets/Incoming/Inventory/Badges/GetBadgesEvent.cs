using System;
using System.Linq;
using System.Text;

using Polar.Communication.Packets.Outgoing.Inventory.Badges;

namespace Polar.Communication.Packets.Incoming.Inventory.Badges
{
    internal class GetBadgesEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new BadgesComposer(Session.GetHabbo().GetBadgeComponent().GetBadges()));
        }
    }
}
