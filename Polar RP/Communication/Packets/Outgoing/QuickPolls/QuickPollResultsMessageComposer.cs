using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.QuickPolls
{
    internal class QuickPollResultsMessageComposer : ServerPacket
    {
        public int YesVotesCount { get; }
        public int NoVotesCount { get; }
        public QuickPollResultsMessageComposer(int yesVotesCount, int noVotesCount)
            : base(ServerPacketHeader.QuickPollResultsMessageComposer)
        {
            this.YesVotesCount = yesVotesCount;
            this.NoVotesCount = noVotesCount;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(-1);
            packet.WriteInteger(2);
            packet.WriteString("1");
            packet.WriteInteger(YesVotesCount);

            packet.WriteString("0");
            packet.WriteInteger(NoVotesCount);
        }
    }
}