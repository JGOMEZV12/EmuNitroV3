using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Quests
{
    internal class CancelQuestEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            PolarEnvironment.GetGame().GetQuestManager().CancelQuest(Session, Packet);
        }
    }
}
