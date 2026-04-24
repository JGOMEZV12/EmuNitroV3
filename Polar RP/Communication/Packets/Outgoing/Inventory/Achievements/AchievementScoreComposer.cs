using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Inventory.Achievements
{
    internal class AchievementScoreComposer : ServerPacket
    {
        public int AchievementScore { get; }

        public AchievementScoreComposer(int achScore)
            : base(ServerPacketHeader.AchievementScoreMessageComposer)
        {
            this.AchievementScore = achScore;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(AchievementScore);
        }
    }
}
