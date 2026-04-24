using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Polar.HabboHotel.Rooms;
namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class PromotableRoomsComposer : ServerPacket
    {
        public ICollection<RoomData> Rooms { get; }

        public PromotableRoomsComposer(ICollection<RoomData> Rooms)
            : base(ServerPacketHeader.PromotableRoomsMessageComposer)
        {
            this.Rooms = Rooms;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(true);
            packet.WriteInteger(Rooms.Count);//Count

            foreach (RoomData Data in Rooms)
            {
                packet.WriteInteger(Data.Id);
                packet.WriteString(Data.Name);
                packet.WriteBoolean(false);
            }
        }
    }
}