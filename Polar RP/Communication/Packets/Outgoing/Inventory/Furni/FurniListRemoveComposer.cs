using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListRemoveComposer : ServerPacket
    {
        public int FurniId { get; }

        public FurniListRemoveComposer(int Id)
            : base(ServerPacketHeader.FurniListRemoveMessageComposer)
        {
            this.FurniId = Id;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(FurniId);
        }
    }
}
