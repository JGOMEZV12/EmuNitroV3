using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Nancy.Session;
using Polar.Communication.Packets.Outgoing.Inventory.Trading;
using System.Net.Sockets;

namespace Polar.Communication.Packets.Incoming.Inventory.Trading
{
    internal class TradingRemoveItemEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room room = PolarEnvironment.GetGame().GetRoomManager().GetRoom(Session.GetHabbo().CurrentRoomId);
            if (room == null)
                return;
            Trade userTrade = room.GetUserTrade(Session.GetHabbo().Id);
            Item userItem = Session.GetHabbo().GetInventoryComponent().GetItem(Packet.PopInt());
            if (userTrade == null || userItem == null)
                return;
            userTrade.TakeBackItem(Session.GetHabbo().Id, userItem);

        }
    }
}