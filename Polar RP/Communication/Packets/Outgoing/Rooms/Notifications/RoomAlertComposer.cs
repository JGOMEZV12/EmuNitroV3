using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Notifications
{
    internal class RoomAlertComposer : ServerPacket
    {
        public string Message { get; }
        public RoomAlertComposer(string Message)
            : base(ServerPacketHeader.RoomAlertComposer)

        {
            this.Message = Message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);
            packet.WriteString(Message);
        }
    }
}