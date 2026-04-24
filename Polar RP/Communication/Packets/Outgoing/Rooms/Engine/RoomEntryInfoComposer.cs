using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{

    internal class RoomEntryInfoComposer : ServerPacket
    {
        public RoomEntryInfoComposer(int roomId, bool isOwner)
            : base(ServerPacketHeader.RoomEntryInfoMessageComposer)
        {
           
            base.WriteInteger(roomId);
            base.WriteBoolean(isOwner);
        }
    }
}