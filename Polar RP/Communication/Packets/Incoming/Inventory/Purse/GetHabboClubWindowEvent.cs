using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.HabboHotel.Catalog;
using Polar.Communication.Packets.Outgoing.Users;

namespace Polar.Communication.Packets.Incoming.Inventory.Purse
{
    internal class GetHabboClubWindowEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            CatalogPage page = PolarEnvironment.GetGame().GetCatalog().TryGetPageByTemplate("vip_buy");
            if (page == null)
                return;

            Session.SendMessage(new GetClubComposer(page, Session, Packet.PopInt()));
        }
    }
}
