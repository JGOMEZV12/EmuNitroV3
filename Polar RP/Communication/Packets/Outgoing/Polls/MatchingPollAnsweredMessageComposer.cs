using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Polls;
using Polar.HabboHotel.Polls.Enums;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Polls
{
    internal class MatchingPollAnsweredMessageComposer : ServerPacket
    {
        public GameClient Session { get; }
        public string text { get; }
        public MatchingPollAnsweredMessageComposer(GameClient Session, string text)
            : base(ServerPacketHeader.QuickPollResultMessageComposer)
        {
            this.Session = Session;
            this.text = text;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            
            packet.WriteInteger(Session.GetHabbo().Id);
            packet.WriteString(text);
            packet.WriteInteger(0);
        }
    }
}
