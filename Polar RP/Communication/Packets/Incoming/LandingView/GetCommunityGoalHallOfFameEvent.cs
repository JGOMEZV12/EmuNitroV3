using Polar.Communication.Packets;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing.LandingView;
using Polar.HabboHotel.GameClients;
using System;

namespace Polar.Communication.Packets.Incoming.LandingView;
internal sealed class GetCommunityGoalHallOfFameEvent : IPacketEvent
{
    public double Delay => 0;

    public void Parse(GameClient session, ClientPacket packet)
    {
        var hof = PolarEnvironment.GetGame().GetHallOfFame();

        session.SendMessage(new HallOfFameComposer(hof));
      
    }
}