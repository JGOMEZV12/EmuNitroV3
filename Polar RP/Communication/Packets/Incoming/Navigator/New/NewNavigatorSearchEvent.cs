using System.Collections.Generic;

using Polar.HabboHotel.Navigator;
using Polar.Communication.Packets.Outgoing.Navigator;

namespace Polar.Communication.Packets.Incoming.Navigator
{
    internal class NewNavigatorSearchEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string Category = Packet.PopString();
            string Unknown = Packet.PopString();

            if (!string.IsNullOrEmpty(Unknown))
            {
                Category = "hotel_view";
                ICollection<SearchResultList> Test = new List<SearchResultList>();

                SearchResultList Null = null;
                if (PolarEnvironment.GetGame().GetNavigator().TryGetSearchResultList(0, out Null))
                {
                    Test.Add(Null);
                    Session.SendMessage(new NavigatorSearchResultSetComposer(Category, Unknown, Test, Session));
                }
            }
            else
            {
                if (Session.GetRoleplay().Chofer || Session.GetRoleplay().IsDead || Session.GetRoleplay().IsJailed || Session.GetRoleplay().Paralized)
                {
                    Session.SendNotification("Lo sentimos, pero no se permite hacer uso del navegador mientras llevas pasajeros, estes muerto, paralizado o encarcelado.");
                    return;
                }

                //Fetch the categorys.
                ICollection<SearchResultList> Test = PolarEnvironment.GetGame().GetNavigator().GetCategorysForSearch(Category);
                if (Test.Count == 0)
                {
                    ICollection<SearchResultList> SecondTest = PolarEnvironment.GetGame().GetNavigator().GetResultByIdentifier(Category);
                    if (SecondTest.Count > 0)
                    {
                        Session.SendMessage(new NavigatorSearchResultSetComposer(Category, Unknown, SecondTest, Session, 2, 100));
                        return;
                    }
                }

                Session.SendMessage(new NavigatorSearchResultSetComposer(Category, Unknown, Test, Session));
            }
        }
    }
}
