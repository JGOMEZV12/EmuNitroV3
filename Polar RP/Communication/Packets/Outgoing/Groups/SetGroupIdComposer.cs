using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class SetGroupIdComposer : ServerPacket
    {
        public int GroupId { get; }
        public SetGroupIdComposer(int Id)
            : base(ServerPacketHeader.SetGroupIdMessageComposer)
        {
            this.GroupId = Id;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GroupId);
        }
    }
}
