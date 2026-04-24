using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class GetCatalogRoomPromotionComposer : ServerPacket
    {
        public List<RoomData> UsersRooms { get; }

        public GetCatalogRoomPromotionComposer(List<RoomData> UsersRooms)
            : base(ServerPacketHeader.PromotableRoomsMessageComposer)
        {
            this.UsersRooms = UsersRooms;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(true);//wat
            packet.WriteInteger(UsersRooms.Count);//Count of rooms?
            foreach (RoomData Room in UsersRooms)
            {
                packet.WriteInteger(Room.Id);
                packet.WriteString(Room.Name);
                packet.WriteBoolean(true);
            }
        }
    }
}