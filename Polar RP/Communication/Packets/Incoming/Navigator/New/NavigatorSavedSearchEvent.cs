using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users.Navigator.SavedSearches;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class NavigatorSavedSearchEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string Filter = Packet.PopString();
            string SearchCode = Packet.PopString();

            if (Session.GetHabbo().GetNavigatorSearches().Searches.Count >= 15)
            {
                Session.SendSimple(1, "Limited");
                return;
            }

            SavedSearch.OnSave(Session, Filter, SearchCode, true);
            Session.GetHabbo().GetNavigatorSearches().Init(Session.GetHabbo());
            ICollection<SavedSearch> ListSearch = Session.GetHabbo().GetNavigatorSearches().Searches;
            Session.SendMessage(new NavigatorSavedSearchComposer(ListSearch));
        }
    }
}