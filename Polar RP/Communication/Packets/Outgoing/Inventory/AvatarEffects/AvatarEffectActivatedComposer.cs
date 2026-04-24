using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Users.Effects;

namespace Polar.Communication.Packets.Outgoing.Inventory.AvatarEffects
{
    internal class AvatarEffectActivatedComposer : ServerPacket
    {
        public AvatarEffect Effect { get; }

        public AvatarEffectActivatedComposer(AvatarEffect Effect)
            : base(ServerPacketHeader.AvatarEffectActivatedMessageComposer)
        {
            this.Effect = Effect;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Effect.SpriteId);
            packet.WriteInteger((int)Effect.Duration);
            packet.WriteBoolean(false);//Permanent
        }
    }
}