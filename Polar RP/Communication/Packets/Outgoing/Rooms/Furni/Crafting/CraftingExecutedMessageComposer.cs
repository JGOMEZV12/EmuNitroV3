using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting
{
    internal class CraftingExecutedMessageComposer : ServerPacket
    {
        public bool Success { get; }
        public string ItemName { get; }
        public CraftingExecutedMessageComposer(bool Success, string ItemName)
            : base(ServerPacketHeader.CraftingExecutedMessageComposer)
        {
            this.Success = Success;
            this.ItemName = ItemName;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(Success);
            packet.WriteString(ItemName);
            packet.WriteString(ItemName);
        }
    }
}
