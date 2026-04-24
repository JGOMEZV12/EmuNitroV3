using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Polls
{

    internal class SuggestPollMessageComposer : ServerPacket
    {
        public Poll poll { get; }
        public SuggestPollMessageComposer(Poll poll)
            : base(ServerPacketHeader.SuggestPollMessageComposer)
        {
            this.poll = poll;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(poll.Id);
            packet.WriteString(poll.PollName); // ?
            packet.WriteString(poll.Thanks); // ?
            packet.WriteString(poll.PollInvitation);
        }
    }
}
