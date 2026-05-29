using System;

using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Incoming;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    public class GetCatalogPageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int PageId = Packet.PopInt();
            int Something = Packet.PopInt();
            string CataMode = Packet.PopString();

            CatalogPage Page = null;
            if (!PolarEnvironment.GetGame().GetCatalog().TryGetPage(PageId, out Page))
                return;

            if (!Page.Enabled || !Page.Visible || Page.MinimumRank > Session.GetHabbo().Rank || (Page.MinimumVIP > Session.GetHabbo().VIPRank && Session.GetHabbo().Rank == 1))
                return;

            //Session.GetHabbo().lastLayout = Page.Template;

            Session.SendMessage(new CatalogPageComposer(Page, Session, Something, CataMode));
        }
    }
}