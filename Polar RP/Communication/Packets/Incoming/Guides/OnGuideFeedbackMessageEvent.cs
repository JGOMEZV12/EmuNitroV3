using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class OnGuideFeedbackMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            bool Feedback = Packet.PopBoolean();

            Session.SendMessage(new OnGuideSessionDetachedComposer(0));

            if (Session != null && Session.GetRoleplay() != null)
            {
                Session.GetRoleplay().Sent911Call = false;
                Session.GetRoleplay().GuideOtherUser = null;
            }
        }
    }
}
