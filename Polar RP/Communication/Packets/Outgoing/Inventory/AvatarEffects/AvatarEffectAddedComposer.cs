using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects
{
    internal class AvatarEffectAddedComposer : ServerPacket
    {
        public int SpriteId { get; }
        public int Duration { get; }

        public AvatarEffectAddedComposer(int SpriteId, int Duration)
            : base(ServerPacketHeader.WardrobeMessageComposer)
        {
            this.SpriteId = SpriteId;
            this.Duration = Duration;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(SpriteId);
            packet.WriteInteger(1);//Types
            packet.WriteInteger(Duration);
            packet.WriteBoolean(false);//Permanent
        }
    }
}
