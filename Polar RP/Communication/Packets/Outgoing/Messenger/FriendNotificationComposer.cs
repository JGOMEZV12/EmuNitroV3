using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Messenger;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class FriendNotificationComposer : ServerPacket
    {
        public int UserId { get; }
        public MessengerEventTypes Type { get; }
        public string Data { get; }

        public FriendNotificationComposer(int UserId, MessengerEventTypes type, string data)
            : base(ServerPacketHeader.FriendNotificationMessageComposer)
        {
            this.UserId = UserId;
            this.Type = type;
            this.Data = data;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(UserId.ToString());
            packet.WriteInteger(MessengerEventTypesUtility.GetEventTypePacketNum(Type));
            packet.WriteString(Data);
        }
    }
}

