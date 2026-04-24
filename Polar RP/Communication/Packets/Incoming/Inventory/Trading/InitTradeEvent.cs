using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Inventory.Trading;
using Polar.Database.Interfaces;


namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class InitTradeEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;

            if (Session.GetHabbo().TradingLockExpiry > 0)
            {
                if (Session.GetHabbo().TradingLockExpiry > PolarEnvironment.GetUnixTimestamp())
                {
                    Session.SendNotification("Actualmente está prohibido operar.");
                    return;
                }
                else
                {
                    Session.GetHabbo().TradingLockExpiry = 0;
                    Session.SendNotification("Su prohibición de comercio ahora ha expirado, por favor, no timo de nuevo.");

                    using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery("UPDATE `user_info` SET `trading_locked` = '0' WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                    }
                }
            }

            RoomUser roomUserByHabbo = room.GetRoomUserManager().GetRoomUserByHabboId(Session.GetHabbo().Id);
            RoomUser roomUserByVirtualId = room.GetRoomUserManager().GetRoomUserByVirtualId(Packet.PopInt());
            if (roomUserByVirtualId == null || roomUserByVirtualId.GetClient() == null || roomUserByVirtualId.GetClient().GetHabbo() == null)
                return;

            if (room.RoomData.TradeSettings == 0)
            {
                Session.SendMessage(new TradingErrorComposer(6, roomUserByVirtualId.GetUsername()));
                return;
            }
            else if (room.RoomData.TradeSettings == 1 && !room.CheckRights(Session))
            {
                Session.SendMessage(new TradingErrorComposer(6, roomUserByVirtualId.GetUsername()));
                return;
            }

            if (roomUserByVirtualId.GetClient().GetHabbo().TradingLockExpiry > 0)
            {
                Session.SendNotification("Vaya, parece que este usuario está actualmente prohibido.");
                return;
            }

            if (roomUserByVirtualId.HasStatus("trd"))
                roomUserByVirtualId.RemoveStatus("trd");
            if (roomUserByHabbo.HasStatus("trd"))
                roomUserByHabbo.RemoveStatus("trd");

            roomUserByVirtualId.SetStatus("trd");
            roomUserByVirtualId.UpdateNeeded = true;
            roomUserByHabbo.SetStatus("trd");
            roomUserByHabbo.UpdateNeeded = true;

            room.TryStartTrade(roomUserByHabbo, roomUserByVirtualId);
        }
    }
}