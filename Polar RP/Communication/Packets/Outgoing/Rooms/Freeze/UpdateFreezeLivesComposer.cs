using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Freeze
{
    internal class UpdateFreezeLivesComposer : ServerPacket
    {
        public int UserId { get; }
        public int FreezeLives { get; }

        public UpdateFreezeLivesComposer(int UserId, int FreezeLives)
            : base(ServerPacketHeader.UpdateFreezeLivesMessageComposer)
        {
            this.UserId = UserId;
            this.FreezeLives = FreezeLives;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserId);
            packet.WriteInteger(FreezeLives);
        }
    }
}
