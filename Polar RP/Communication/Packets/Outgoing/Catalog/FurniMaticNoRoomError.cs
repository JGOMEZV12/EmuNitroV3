using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class FurniMaticNoRoomError : ServerPacket
    {
        public FurniMaticNoRoomError()
           : base(ServerPacketHeader.ReloadRecyclerComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket response)
        {
            response.WriteInteger(1);
            response.WriteInteger(0);
        }
    }
}
