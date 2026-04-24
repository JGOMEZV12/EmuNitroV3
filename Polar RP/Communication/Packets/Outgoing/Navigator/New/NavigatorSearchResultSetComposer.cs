using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Navigator;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorSearchResultSetComposer : ServerPacket
    {
        public string Category { get; }
        public string Data { get; }
        public ICollection<SearchResultList> SearchResultLists { get; }
        public GameClient Session { get; }
        public int GoBack { get; }
        public int FetchLimit { get; }
        public NavigatorSearchResultSetComposer(string category, string data, ICollection<SearchResultList> searchResultLists, GameClient session, int goBack = 1, int fetchLimit = 25)
            : base(ServerPacketHeader.NavigatorSearchResultSetMessageComposer)
        {
            this.Category = category;
            this.Data = data;
            this.SearchResultLists = searchResultLists;
            this.Session = session;
            this.GoBack = goBack;
            this.FetchLimit = fetchLimit;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Category);//Search code.
            packet.WriteString(Data);//Text?

            packet.WriteInteger(SearchResultLists.Count);//Count
            foreach (SearchResultList SearchResult in SearchResultLists.ToList())
            {
               packet.WriteString(SearchResult.CategoryIdentifier);
               packet.WriteString(SearchResult.PublicName);
                packet.WriteInteger(NavigatorSearchAllowanceUtility.GetIntegerValue(SearchResult.SearchAllowance) != 0 ? GoBack : NavigatorSearchAllowanceUtility.GetIntegerValue(SearchResult.SearchAllowance));//0 = nothing, 1 = show more, 2 = back Action allowed.
                packet.WriteBoolean(false);//True = minimized, false = open.
                packet.WriteInteger(SearchResult.ViewMode == NavigatorViewMode.REGULAR ? 0 : SearchResult.ViewMode == NavigatorViewMode.THUMBNAIL ? 1 : 0);//View mode, 0 = tiny/regular, 1 = thumbnail

                NavigatorHandler.Search(packet, SearchResult, Data, Session, FetchLimit);
            }
        }
    }
}
