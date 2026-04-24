using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users.Effects;

namespace Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects
{
    internal class AvatarEffectExpiredComposer : ServerPacket
    {
        public AvatarEffect Effect { get; }

        public AvatarEffectExpiredComposer(AvatarEffect Effect)
            : base(ServerPacketHeader.AvatarEffectExpiredMessageComposer)
        {
            this.Effect = Effect;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Effect.SpriteId);
        }
    }
}
