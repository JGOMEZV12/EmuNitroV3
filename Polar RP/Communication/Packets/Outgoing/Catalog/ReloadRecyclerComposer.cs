using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class ReloadRecyclerComposer : ServerPacket
    {
        public ReloadRecyclerComposer()
            : base(ServerPacketHeader.ReloadRecyclerComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(1);
            packet.WriteInteger(0);
        }
    }
}