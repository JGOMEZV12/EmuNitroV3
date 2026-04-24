using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Polls
{
    internal class MatchingPollResultMessageComposer : ServerPacket
    {
        public Poll poll { get; }
        public MatchingPollResultMessageComposer(Poll poll)
            : base(ServerPacketHeader.QuickPollResultsMessageComposer)
        {
            this.poll = poll;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(poll.Id);
            packet.WriteInteger(2);
            packet.WriteString("0");
            packet.WriteInteger(poll.AnswersNegative);
            packet.WriteString("1");
            packet.WriteInteger(poll.AnswersPositive);
        }
    }
}
