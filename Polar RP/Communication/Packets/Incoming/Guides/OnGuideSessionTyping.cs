using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class OnGuideSessionTyping : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            bool Typing = Packet.PopBoolean();

            if (Session != null && Session.GetRoleplay() != null && Session.GetRoleplay().GuideOtherUser != null)
                Session.GetRoleplay().GuideOtherUser.SendMessage(new OnGuideSessionPartnerIsTypingComposer(Typing));
        }
    }
}
