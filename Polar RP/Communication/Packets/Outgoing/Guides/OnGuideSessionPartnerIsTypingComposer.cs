using System;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class OnGuideSessionPartnerIsTypingComposer : ServerPacket
    {
        public bool Typing { get; }
        public OnGuideSessionPartnerIsTypingComposer(bool Typing)
            : base(ServerPacketHeader.OnGuideSessionPartnerIsTypingComposer)
        {
            this.Typing = Typing;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(Typing);
        }
    }
}