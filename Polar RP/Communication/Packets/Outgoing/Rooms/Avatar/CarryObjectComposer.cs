using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Avatar
{
    internal class CarryObjectComposer : ServerPacket
    {
        public int VirtualId { get; }
        public int ItemId { get; }

        public CarryObjectComposer(int virtualID, int itemID)
            : base(ServerPacketHeader.CarryObjectMessageComposer)
        {
            this.VirtualId = virtualID;
            this.ItemId = itemID;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(VirtualId);
            packet.WriteInteger(ItemId);
        }
    }
}
