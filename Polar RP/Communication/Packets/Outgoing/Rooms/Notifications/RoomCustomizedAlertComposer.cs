using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Notifications
{
    internal class RoomAlertxComposer : ServerPacket
    {
        public string Message { get; }
        public RoomAlertxComposer(string message)
            : base(ServerPacketHeader.RoomAlertComposer)

        {
            this.Message = message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);
            packet.WriteString(Message);
        }
    }
}