using Polar.HabboHotel.Users.Navigator.SavedSearches;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class NavigatorSavedSearchComposer : ServerPacket
    {
        public ICollection<SavedSearch> Saved { get; }
        public NavigatorSavedSearchComposer(ICollection<SavedSearch> Saved)
            : base(ServerPacketHeader.NavigatorSavedSearchMessageComposer)
        {
            this.Saved = Saved;
            Compose(this);
        }


        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Saved.Count);
            foreach (SavedSearch Save in Saved)
            {
                packet.WriteInteger(Save.Id);
                packet.WriteString(Save.Filter);
                packet.WriteString(Save.Search);
                packet.WriteString("");
            }
        }
    }
}