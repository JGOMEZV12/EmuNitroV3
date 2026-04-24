using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class GroupFurniSettingsComposer : ServerPacket
    {
        public Group Group { get; }
        public int ItemId { get; }
        public int UserId { get; }

        public GroupFurniSettingsComposer(Group group, int itemId, int userId)
            : base(ServerPacketHeader.GroupFurniSettingsMessageComposer)
        {
            this.Group = group;
            this.ItemId = itemId;
            this.UserId = userId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ItemId);//Item Id
            packet.WriteInteger(Group.Id);//Group Id?
            packet.WriteString(Group.Name);
            packet.WriteInteger(Group.RoomId);//RoomId
            packet.WriteBoolean(Group.IsMember(UserId));//Member?
            packet.WriteBoolean(Group.ForumEnabled);//Has a forum
        }
    }
}