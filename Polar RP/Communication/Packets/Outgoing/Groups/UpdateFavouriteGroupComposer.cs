using System;
using System.Linq;
using System.Text;

using Polar.HabboHotel.Groups;

namespace Polar.Communication.Packets.Outgoing.Groups
{
    internal class UpdateFavouriteGroupComposer : ServerPacket
    {
        public Group Group { get; }
        public int VirtualId { get; }

        public UpdateFavouriteGroupComposer(Group Group, int VirtualId)
            : base(ServerPacketHeader.UpdateFavouriteGroupMessageComposer)
        {
            this.Group = Group;
            this.VirtualId = VirtualId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);//Sends 0 on .COM
            packet.WriteInteger(Group != null ? Group.Id : 0);
            packet.WriteInteger(3);
            packet.WriteString(Group != null ? Group.Name : string.Empty);
        }
    }
}

