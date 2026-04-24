using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.LandingView;

namespace Polar.Communication.Packets.Incoming.Quests
{
    internal class GetDailyQuestEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            int UsersOnline = PolarEnvironment.GetGame().GetClientManager().Count;

            Session.SendMessage(new ConcurrentUsersGoalProgressComposer(UsersOnline));
        }
    }
}
