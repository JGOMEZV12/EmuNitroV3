using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class NewBuddyRequestComposer : ServerPacket
    {
        public UserCache UserCache { get; }

        public NewBuddyRequestComposer(UserCache Habbo)
            : base(ServerPacketHeader.NewBuddyRequestMessageComposer)
        {
            this.UserCache = Habbo;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserCache.Id);
            packet.WriteString(UserCache.Username);
            packet.WriteString(UserCache.Look);
        }
    }
}
