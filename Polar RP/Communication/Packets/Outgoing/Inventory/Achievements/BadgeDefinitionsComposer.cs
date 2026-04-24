using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Achievements;

namespace Polar.Communication.Packets.Outgoing.Inventory.Achievements
{
    internal class BadgeDefinitionsComposer : ServerPacket
    {
        public Dictionary<string, Achievement> Achievements { get; }

        public BadgeDefinitionsComposer(Dictionary<string, Achievement> Achievements)
            : base(ServerPacketHeader.BadgeDefinitionsMessageComposer)
        {
            this.Achievements = Achievements;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Achievements.Count);

            foreach (Achievement Achievement in Achievements.Values)
            {
                packet.WriteString(Achievement.GroupName.Replace("ACH_", ""));
                packet.WriteInteger(Achievement.Levels.Count);
                foreach (AchievementLevel Level in Achievement.Levels.Values)
                {
                    packet.WriteInteger(Level.Level);
                    packet.WriteInteger(Level.Requirement);
                }
            }
        }
    }
}
