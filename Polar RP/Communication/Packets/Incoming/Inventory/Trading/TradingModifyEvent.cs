
using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class TradingModifyEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket packet)
        {
            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            Trade userTrade = room.GetUserTrade(Session.GetHabbo().Id);
            if (userTrade == null)
                return;
            userTrade.Unaccept(Session.GetHabbo().Id);
        }
    }
}