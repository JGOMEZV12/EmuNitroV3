using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Polls
{
    internal class MatchingPollMessageComposer : ServerPacket
    {
        public Poll poll { get; }
        public MatchingPollMessageComposer(Poll poll)
            : base(ServerPacketHeader.QuickPollMessageComposer)
        {
            this.poll = poll;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString("MATCHING_POLL");
            packet.WriteInteger(poll.Id);
            packet.WriteInteger(poll.Id);
            packet.WriteInteger(15580);
            packet.WriteInteger(poll.Id);
            packet.WriteInteger(29);
            packet.WriteInteger(5);
            packet.WriteString(poll.PollName);
        }
    }
}
