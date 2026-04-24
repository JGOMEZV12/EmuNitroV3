using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Settings
{
    internal class FlatControllerAddedComposer : ServerPacket
    {
        public int RoomId { get; }
        public int UserId { get; }
        public string Username { get; }
        public FlatControllerAddedComposer(int RoomId, int UserId, string Username)
            : base(ServerPacketHeader.FlatControllerAddedMessageComposer)
        {
            this.RoomId = RoomId;
            this.UserId = UserId;
            this.Username = Username;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
            packet.WriteInteger(UserId);
            packet.WriteString(Username);
        }
    }
}

