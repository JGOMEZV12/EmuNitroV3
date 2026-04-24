using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Avatar
{
    internal class DanceComposer : ServerPacket
    {
        public RoomUser Avatar { get; }
        public int Dance { get; }

        public DanceComposer(RoomUser avatar, int Dance)
            : base(ServerPacketHeader.DanceMessageComposer)
        {
            this.Avatar = avatar;
            this.Dance = Dance;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Avatar.VirtualId);
            packet.WriteInteger(Dance);
        }
    }
}