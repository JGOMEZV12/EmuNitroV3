using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Session
{
    internal class RoomReadyComposer : ServerPacket
    {
        public int RoomId { get; }
        public string Model { get; }
        public string image { get; }
        public int messageType { get; }
        public string message { get; }
        public RoomReadyComposer(int RoomId, string Model)
            : base(ServerPacketHeader.RoomReadyMessageComposer)
        {
            this.RoomId = RoomId;
            this.Model = Model;
            Compose(this);
        }

        public RoomReadyComposer(string image, int messageType, string message)
    : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            this.image = image;
            this.messageType = messageType;
            this.message = message;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if(image == null)
            {
                packet.WriteString(Model);
                packet.WriteInteger(RoomId);
            }else
            {
                packet.WriteString(image);
                packet.WriteInteger(messageType);
                packet.WriteString("display");
                packet.WriteString("BUBBLE");
                packet.WriteString("message");
                packet.WriteString(message);
                packet.WriteString("linkUrl");
                packet.WriteString("");
            }
            
        }
    }
}
