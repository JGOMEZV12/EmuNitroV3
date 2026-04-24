using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Notifications
{
    internal class RoomNotificationComposer : ServerPacket
    {
        public string Type { get; }
        public string Key { get; }
        public string Value { get; }

        public string Title { get; }
        public string Message { get; }
        public string Text { get; }
        public string Image { get; }
        public string HotelName { get; }
        public string HotelUrl { get; }
        public Dictionary<string, string> Keys { get; }
        public bool Bubble { get; }
        public GameClient Session { get; }
        public Room Room { get; }
        public RoomNotificationComposer(string type, string key, string value) : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Type = type;
            Key = key;
            Value = value;
            Compose(this);
        }

        public RoomNotificationComposer(string type)
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Type = type;
            Compose(this);
        }

        public RoomNotificationComposer(string title, string message, string image, string hotelName = "", string hotelURL = "")
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Title = title;
            Message = message;
            Image = image;
            HotelName = hotelName;
            HotelUrl = hotelURL;
            Compose(this);
        }

        public RoomNotificationComposer(string type, Dictionary<string, string> keys)
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Type = type;
            Keys = keys;
            Compose(this);
        }

        public RoomNotificationComposer(string text, string image)
            : base(ServerPacketHeader.RoomNotificationMessageComposer)
        {
            Image = image;
            Text = text;
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            if (!string.IsNullOrEmpty(Message))
            {
                packet.WriteString(Image);
                packet.WriteInteger(string.IsNullOrEmpty(HotelName) ? 2 : 4);
                packet.WriteString("title");
                packet.WriteString(Title);
                packet.WriteString("message");
                packet.WriteString(Message);

                if (!string.IsNullOrEmpty(HotelName))
                {
                    packet.WriteString("linkUrl");
                    packet.WriteString(HotelUrl);
                    packet.WriteString("linkTitle");
                    packet.WriteString(HotelName);
                }
            }
            else if (!string.IsNullOrEmpty(Key))
            {
                packet.WriteString(Type);
                packet.WriteInteger(1);//Count
                {
                    packet.WriteString(Key);//Type of message
                    packet.WriteString(Value);
                }
            }
            else if (!string.IsNullOrEmpty(Keys.ToString()))
            {
                packet.WriteString(Type);
                packet.WriteInteger(Keys.Count);//Count
                foreach (KeyValuePair<string, string> current in Keys)
                {
                    packet.WriteString(current.Key);//Type of message
                    packet.WriteString(current.Value);
                }
            }
            else if (!string.IsNullOrEmpty(Text))
            {
                packet.WriteString(Image);
                packet.WriteInteger(2);
                packet.WriteString("message");
                packet.WriteString(Text);
                packet.WriteString("display");
                packet.WriteString("BUBBLE");
            }
            else
            {

                packet.WriteString(Type);
                packet.WriteInteger(0);//Count
            }
        }
    }
}
