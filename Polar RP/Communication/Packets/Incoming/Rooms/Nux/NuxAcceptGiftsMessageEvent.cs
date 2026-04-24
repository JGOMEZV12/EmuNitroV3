using System;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;
using Polar.Utilities;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;
using Polar.Communication.Packets.Outgoing.Rooms.Nux;

namespace Polar.Communication.Packets.Incoming.Rooms.Nux
{
    internal class NuxAcceptGiftsMessageEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new NuxItemListComposer());
        }
    }
}