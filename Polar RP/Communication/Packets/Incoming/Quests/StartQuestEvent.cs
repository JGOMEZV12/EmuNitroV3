using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Incoming.Quests
{
    internal class StartQuestEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int QuestId = Packet.PopInt();

            PolarEnvironment.GetGame().GetQuestManager().ActivateQuest(Session, QuestId);
        }
    }
}
