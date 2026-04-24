using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class UserRemoveComposer : ServerPacket
    {
        public int UserId { get; }

        public UserRemoveComposer(int Id)
            : base(ServerPacketHeader.UserRemoveMessageComposer)
        {
            this.UserId = Id;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(UserId.ToString());
        }
    }
}
