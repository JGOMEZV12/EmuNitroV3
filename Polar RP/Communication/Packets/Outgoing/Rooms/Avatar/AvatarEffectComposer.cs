using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Rooms.Avatar
{
    internal class AvatarEffectComposer : ServerPacket
    {
        public int PlayerId { get; }
        public int EffectId { get; }

        public AvatarEffectComposer(int playerID, int effectID)
            : base(ServerPacketHeader.AvatarEffectMessageComposer)
        {
            this.PlayerId = playerID;
            this.EffectId = effectID;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(PlayerId);
            packet.WriteInteger(EffectId);
            packet.WriteInteger(0);
        }
    }
}