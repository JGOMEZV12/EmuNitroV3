using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.Navigator
{
    internal class RoomRatingComposer : ServerPacket
    {
        public int Score { get; }
        public bool CanVote { get; }

        public RoomRatingComposer(int score, bool canVote)
            : base(ServerPacketHeader.RoomRatingMessageComposer)
        {
            this.Score = score;
            this.CanVote = canVote;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Score);
            packet.WriteBoolean(CanVote);
        }
    }
}
