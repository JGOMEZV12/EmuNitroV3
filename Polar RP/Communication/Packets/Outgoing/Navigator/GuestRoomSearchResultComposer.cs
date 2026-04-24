using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class GuestRoomSearchResultComposer : ServerPacket
    {
        public int Mode { get; }
        public string UserQuery { get; }
        public ICollection<RoomData> Rooms { get; }
        public GuestRoomSearchResultComposer(int mode, string userQuery, ICollection<RoomData> rooms)
           : base(ServerPacketHeader.GuestRoomSearchResultMessageComposer)
        {
            this.Mode = mode;
            this.UserQuery = userQuery;
            this.Rooms = rooms;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Mode);
          packet.WriteString(UserQuery);
         
           packet.WriteInteger(Rooms.Count);
           foreach (RoomData data in Rooms)
           {
               RoomAppender.WriteRoom(packet, data, data.Promotion);
           }

           packet.WriteBoolean(false);
       }
    }
}
