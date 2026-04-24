using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class VisitRoomGuides : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session.GetRoleplay().GuideOtherUser == null)
                return;

            GameClient requester = Session.GetRoleplay().GuideOtherUser;
            Session.SendMessage(new RoomForwardComposer(requester.GetHabbo().CurrentRoomId));
        }
    }
}
