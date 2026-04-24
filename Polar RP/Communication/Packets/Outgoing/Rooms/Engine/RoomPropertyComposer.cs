using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class RoomPropertyComposer : ServerPacket
    {
        public string Name { get; }
        public string Val { get; }

        public RoomPropertyComposer(string name, string val)
            : base(ServerPacketHeader.RoomPropertyMessageComposer)
        {
            this.Name = name;
            this.Val = val;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Name);
            packet.WriteString(Val);
        }
    }
}
