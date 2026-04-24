using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Nancy.Session;
using Polar.Communication.Packets.Outgoing.Inventory.Trading;

namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class TradingCancelEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket packet)
        {
            if (Session == null || Session.GetHabbo() == null || !Session.GetHabbo().InRoom)
                return;

            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;

            room.TryStopTrade(Session.GetHabbo().Id);
        }
    }
}