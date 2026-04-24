using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class TradingCancelConfirmEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket packet)
        {
            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            room.TryStopTrade(Session.GetHabbo().Id);
        }
    }
}
