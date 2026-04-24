using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.HabboCamera
{
    internal class CamereFinishPurchaseComposer : ServerPacket
    {
        public CamereFinishPurchaseComposer()
            : base(ServerPacketHeader.CamereFinishPurchaseMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        { 
        }
    }
}
