using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Nux
{
    internal class NuxUserStatus : ServerPacket
    {
        public bool Noob { get; }
        public NuxUserStatus(bool noob = false)
            : base(ServerPacketHeader.NuxUserStatus)
        {
            this.Noob = noob;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            if(Noob == true)
            {
                packet.WriteInteger(0);
            }
            else {
                packet.WriteInteger(2);
            }
        }
    }
}
