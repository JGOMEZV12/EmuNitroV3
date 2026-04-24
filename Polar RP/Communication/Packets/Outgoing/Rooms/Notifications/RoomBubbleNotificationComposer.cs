using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Notifications
{
    internal class RoomBubbleNotificationComposer : ServerPacket

    {
        public string Message { get; }
        public string Image { get; }
        public string HotelUrl { get; }
        public Dictionary<string, string> Keys { get;  }
        
        public RoomBubbleNotificationComposer(string Image, string Message, string linkUrl = "")
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            this.Message = Message;
            this.Image = Image;
            this.HotelUrl = linkUrl;
            Compose(this);
        }

        public void Compose(ServerPacket bubbleNotification)
        {
            bubbleNotification.WriteString(Image);
            bubbleNotification.WriteInteger(string.IsNullOrEmpty(HotelUrl) ? 2 : 3);
            bubbleNotification.WriteString("display");
            bubbleNotification.WriteString("BUBBLE");
            bubbleNotification.WriteString("message");
            bubbleNotification.WriteString(Message);
            if (string.IsNullOrEmpty(HotelUrl)) { 
            }
            bubbleNotification.WriteString("linkUrl");
            bubbleNotification.WriteString(HotelUrl);
        }
    }
}
