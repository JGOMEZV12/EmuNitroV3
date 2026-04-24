using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Quests
{
    internal class QuestAbortedComposer : ServerPacket
    {
        public QuestAbortedComposer()
            : base(ServerPacketHeader.QuestAbortedMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(false);
        }
    }
}
