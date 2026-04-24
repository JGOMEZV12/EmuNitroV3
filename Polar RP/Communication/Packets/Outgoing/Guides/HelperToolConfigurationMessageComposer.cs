using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Guides;

namespace Polar.Communication.Packets.Outgoing.Guides
{
    internal class HelperToolConfigurationComposer : ServerPacket
    {
        public GameClient Session { get; }
        public HelperToolConfigurationComposer(GameClient Session)
            : base(ServerPacketHeader.HelperToolConfigurationComposer)
        {
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            GuideManager guideManager = PolarEnvironment.GetGame().GetGuideManager();

            if (Session == null || Session.GetRoleplay() == null)
                packet.WriteBoolean(false);
            else
                packet.WriteBoolean(Session.GetRoleplay().IsWorking);
            packet.WriteInteger(guideManager.GuidesCount);
            packet.WriteInteger(guideManager.HelpersCount);
            packet.WriteInteger(guideManager.GuardiansCount);
        }
    }
}