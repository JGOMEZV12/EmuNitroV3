using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class FavouritesComposer : ServerPacket
    {
        public ArrayList FavouriteIds { get; }

        public FavouritesComposer(ArrayList favouriteIds)
            : base(ServerPacketHeader.FavouritesMessageComposer)
        {
            this.FavouriteIds = favouriteIds;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(50);
            packet.WriteInteger(FavouriteIds.Count);

            foreach (int id in FavouriteIds.ToArray())
            {
                packet.WriteInteger(id);
            }
        }
    }
}
