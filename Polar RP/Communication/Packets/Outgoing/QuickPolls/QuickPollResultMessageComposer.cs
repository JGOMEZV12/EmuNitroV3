using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.QuickPolls
{
    internal class QuickPollResultMessageComposer : ServerPacket
    {
        public int UserId { get; }
        public string MyVote { get; }
        public int YesVotesCount { get; }
        public int NoVotesCount { get; }

        public QuickPollResultMessageComposer(int userId, String myVote, int yesVotesCount, int noVotesCount)
            : base(ServerPacketHeader.QuickPollResultMessageComposer)
        {
            this.UserId = userId;
            this.MyVote = myVote;
            this.YesVotesCount = yesVotesCount;
            this.NoVotesCount = noVotesCount;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(UserId);
            packet.WriteString(MyVote);
            packet.WriteInteger(2);
            packet.WriteString("1");
            packet.WriteInteger(YesVotesCount);

            packet.WriteString("0");
            packet.WriteInteger(NoVotesCount);
        }
    }
}