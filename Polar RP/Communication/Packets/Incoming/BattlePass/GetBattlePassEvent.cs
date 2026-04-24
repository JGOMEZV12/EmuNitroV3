using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.BattlePass;
using Polar.Communication.Packets.Outgoing.BattlePass;

namespace Polar.Communication.Packets.Incoming.BattlePass
{
    public class GetBattlePassEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (session == null || session.GetHabbo() == null || session.GetRoleplay() == null)
                return;

            BattlePassManager manager = PolarEnvironment.GetGame().GetBattlePassManager();
            session.SendMessage(new BattlePassComposer(session, manager));
        }
    }
}
