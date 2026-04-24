using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class RespectNotificationComposer : ServerPacket
    {
        public int UserId { get; }
        public int Respect { get; }
        public RespectNotificationComposer(int userId, int respect)
            : base(ServerPacketHeader.RespectNotificationMessageComposer)
        {
            this.UserId = userId;
            this.Respect = respect;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserId);
            packet.WriteInteger(Respect);
        }
    }
}
