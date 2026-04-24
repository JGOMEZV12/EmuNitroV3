using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Users.Navigator.SavedSearches;
using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class DeleteNavigatorSavedSearchEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int searchId = Packet.PopInt();
            var ListSaved = new SavedSearch(0, "", "");

            SavedSearch.OnDelete(Session, true, searchId);
            //Console.WriteLine("SearchId: " + searchId);
            Session.GetHabbo().GetNavigatorSearches().Init(Session.GetHabbo());
            ICollection<SavedSearch> ListSearch = Session.GetHabbo().GetNavigatorSearches().Searches;
            Session.SendMessage(new NavigatorSavedSearchComposer(ListSearch));
        }
    }
}