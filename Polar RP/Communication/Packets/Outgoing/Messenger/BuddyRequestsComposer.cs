using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Cache;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class BuddyRequestsComposer : ServerPacket
    {
        public ICollection<MessengerRequest> Requests { get; }

        public BuddyRequestsComposer(ICollection<MessengerRequest> requests)
            : base(ServerPacketHeader.BuddyRequestsMessageComposer)
        {
            this.Requests = requests;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Requests.Count);
            packet.WriteInteger(Requests.Count);

            foreach (MessengerRequest Request in Requests)
            {
                packet.WriteInteger(Request.From);
                packet.WriteString(Request.Username);

                UserCache User = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(Request.From);
                packet.WriteString(User != null ? User.Look : "");
            }
        }
    }
}
