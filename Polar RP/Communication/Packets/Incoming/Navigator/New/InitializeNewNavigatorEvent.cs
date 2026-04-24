using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Users.Navigator.SavedSearches;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Navigator;
using Polar.Communication.Packets.Outgoing.Navigator;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class InitializeNewNavigatorEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            ICollection<TopLevelItem> TopLevelItems = PolarEnvironment.GetGame().GetNavigator().GetTopLevelItems();
            ICollection<SearchResultList> SearchResultLists = PolarEnvironment.GetGame().GetNavigator().GetSearchResultLists();
            ICollection<SavedSearch> ListSearch = Session.GetHabbo().GetNavigatorSearches().Searches;

            Session.SendMessage(new NavigatorMetaDataParserComposer(TopLevelItems));
            Session.SendMessage(new NavigatorLiftedRoomsComposer());
            Session.SendMessage(new NavigatorCollapsedCategoriesComposer());
            Session.SendMessage(new NavigatorPreferencesComposer());
            Session.SendMessage(new NavigatorSavedSearchComposer(ListSearch));
        }
    }
}
