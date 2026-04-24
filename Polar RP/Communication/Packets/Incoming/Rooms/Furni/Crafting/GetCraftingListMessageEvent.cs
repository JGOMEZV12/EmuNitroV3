using System;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Crafting
{
    internal class GetCraftingListMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int count = Packet.PopInt();
        }
    }
}
