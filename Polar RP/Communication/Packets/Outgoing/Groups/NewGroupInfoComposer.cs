using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class NewGroupInfoComposer : ServerPacket
    {
        public int RoomId { get; }
        public int GroupId { get; }

        public NewGroupInfoComposer(int RoomId, int GroupId)
            : base(ServerPacketHeader.NewGroupInfoMessageComposer)
        {
            this.RoomId = RoomId;
            this.GroupId = GroupId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(RoomId);
            packet.WriteInteger(GroupId);
        }
    }
}
