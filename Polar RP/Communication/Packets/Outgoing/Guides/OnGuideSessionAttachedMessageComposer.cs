using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionAttachedComposer : ServerPacket
    {
        public int userid { get; }
        public string message { get; }
        public int type { get; }
        public OnGuideSessionAttachedComposer(int userid, string message, int type)
            : base(ServerPacketHeader.OnGuideSessionAttachedComposer)
        {
            this.userid = userid;
            this.message = message;
            this.type = type;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if (type == 30)
                packet.WriteBoolean(false);
            else
                packet.WriteBoolean(true);
            packet.WriteInteger(userid);
            packet.WriteString(message);
            packet.WriteInteger(type);
        }
    }
}