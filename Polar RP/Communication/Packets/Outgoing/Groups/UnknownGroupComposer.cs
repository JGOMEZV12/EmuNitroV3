using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class UnknownGroupComposer : ServerPacket
    {
        public int GroupId { get; }
        public int HabboId { get; }

        public UnknownGroupComposer(int GroupId, int HabboId)
            : base(ServerPacketHeader.UnknownGroupMessageComposer)
        {
            this.GroupId = GroupId;
            this.HabboId = HabboId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GroupId);
            packet.WriteInteger(HabboId);
        }
    }
}