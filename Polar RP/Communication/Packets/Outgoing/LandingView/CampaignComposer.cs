using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.LandingView
{
    internal class CampaignComposer : ServerPacket
    {
        public string CampaignString { get; }
        public string CampaignName { get; }

        public CampaignComposer(string campaignString, string campaignName)
            : base(ServerPacketHeader.CampaignMessageComposer)
        {
            this.CampaignName = campaignName;
            this.CampaignString = campaignString;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(CampaignString);
            packet.WriteString(CampaignName);
        }
    }
}
