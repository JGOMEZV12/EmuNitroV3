using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Polar.HabboHotel.Users;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupMemberUpdatedComposer : ServerPacket
    {
        public int GroupId { get; }
        public Habbo Habbo { get; }
        public int Type { get; }

        public GroupMemberUpdatedComposer(int GroupId, Habbo Habbo, int Type)
            : base(ServerPacketHeader.GroupMemberUpdatedMessageComposer)
        {
            this.GroupId = GroupId;
            this.Habbo = Habbo;
            this.Type = Type;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(GroupId);//GroupId
            packet.WriteInteger(Type);//Type?
            {
                packet.WriteInteger(Habbo.Id);//UserId
                packet.WriteString(Habbo.Username);
                packet.WriteString(Habbo.Look);
                packet.WriteString(string.Empty);
            }
        }
    }
}
