using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    internal class FurniMaticRewardsComposer : ServerPacket
    {
        public FurniMaticRewardsComposer()
           : base(ServerPacketHeader.FurniMaticRewardsComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket response)
        {
            response.WriteInteger(5);
            for (int i = 5; i >= 1; i--)
            {
                response.WriteInteger(i);
                if (i <= 1) response.WriteInteger(1);
                else if (i == 2) response.WriteInteger(5);
                else if (i == 3) response.WriteInteger(20);
                else if (i == 4) response.WriteInteger(50);
                else if (i == 5) response.WriteInteger(100);

                var rewards = PolarEnvironment.GetGame().GetFurniMaticRewardsMnager().GetRewardsByLevel(i);
                response.WriteInteger(rewards.Count);
                foreach (var reward in rewards)
                {
                    response.WriteString(reward.GetBaseItem().ItemName);
                    response.WriteInteger(reward.DisplayId);
                    response.WriteString(reward.GetBaseItem().Type.ToString().ToLower());
                    response.WriteInteger(reward.GetBaseItem().SpriteId);
                }
            }
        }
    }
}
