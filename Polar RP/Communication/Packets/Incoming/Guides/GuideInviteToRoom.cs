using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class GuideInviteToRoom : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            GameClient requester = Session.GetRoleplay().GuideOtherUser;

            requester.SendMessage(new OnGuideSessionInvitedToGuideRoomComposer(Session));
            Session.SendMessage(new OnGuideSessionInvitedToGuideRoomComposer(Session));
        }
    }
}
