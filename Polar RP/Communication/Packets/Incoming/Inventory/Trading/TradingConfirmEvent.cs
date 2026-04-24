using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class TradingConfirmEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().InRoom)
                return;

            Room Room;

            if (!PolarEnvironment.GetGame().GetRoomManager().TryGetRoom(Session.GetHabbo().CurrentRoomId, out Room))
                return;

            if (!Room.CanTradeInRoom)
                return;

            Trade Trade = Room.GetUserTrade(Session.GetHabbo().Id);
            if (Trade == null)
                return;

            Console.WriteLine("Bien bro");
            Trade.CompleteTrade(Session.GetHabbo().Id);
        }
    }
}