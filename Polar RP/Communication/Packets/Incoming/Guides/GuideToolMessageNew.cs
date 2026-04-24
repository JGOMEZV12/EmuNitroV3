using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class GuideToolMessageNew : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            string message = Packet.PopString();
            GameClient requester = Session.GetRoleplay().GuideOtherUser;

            requester.SendMessage(new OnGuideSessionMsgComposer(Session, message));
            Session.SendMessage(new OnGuideSessionMsgComposer(Session, message));
        }
    }
}
