using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Inventory.Achievements;

namespace Polar.Communication.Packets.Incoming.Inventory.Achievements
{
    internal class GetAchievementsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new AchievementsComposer(Session, PolarEnvironment.GetGame().GetAchievementManager()._achievements.Values.ToList()));
        }
    }
}
