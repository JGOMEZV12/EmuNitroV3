using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;

namespace Polar.Communication.Packets.Outgoing.Messenger
{
    internal class MessengerInitComposer : ServerPacket
    {
        public MessengerInitComposer()
            : base(ServerPacketHeader.MessengerInitMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PolarStaticGameSettings.MessengerFriendLimit);//Friends max.
            packet.WriteInteger(300);
            packet.WriteInteger(800);
            //packet.WriteInteger(1100);
            packet.WriteInteger(2); // category count
            packet.WriteInteger(1);
            packet.WriteString("Pandillas");
            packet.WriteInteger(2);
            packet.WriteString("Staff");
        }
    }
}
