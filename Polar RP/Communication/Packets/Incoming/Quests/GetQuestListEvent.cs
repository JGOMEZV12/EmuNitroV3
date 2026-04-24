using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Quests;
using Polar.Communication.Packets.Incoming;

namespace Polar.Communication.Packets.Incoming.Quests
{
    public class GetQuestListEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            PolarEnvironment.GetGame().GetQuestManager().GetList(Session, null);
        }
    }
}