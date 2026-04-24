using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class FlatCreatedComposer : ServerPacket
    {
        public int RoomId { get; }
        public string RoomName { get; }

        public FlatCreatedComposer(int roomId, string roomName)
            : base(ServerPacketHeader.FlatCreatedMessageComposer)
        {
            this.RoomId = roomId;
            this.RoomName = roomName;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
            packet.WriteString(RoomName);
        }
    }
}