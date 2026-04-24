using System;

using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.Communication.Packets.Incoming.Guides
{
    internal class CancellInviteGuide : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new OnGuideSessionDetachedComposer(2));
        }
    }
}
