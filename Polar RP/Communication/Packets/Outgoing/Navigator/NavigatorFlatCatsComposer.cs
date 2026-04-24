using Polar.HabboHotel.Navigator;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    class NavigatorFlatCatsComposer : ServerPacket
    {
        public NavigatorFlatCatsComposer(ICollection<SearchResultList> Categories, int Rank)
            : base(ServerPacketHeader.NavigatorFlatCatsMessageComposer)
        {
            base.WriteInteger(Categories.Count);
            foreach (SearchResultList Category in Categories.ToList())
            {
                base.WriteInteger(Category.Id);
                base.WriteString(Category.PublicName);
                base.WriteBoolean(true);//TODO
            }
        }
    }
}