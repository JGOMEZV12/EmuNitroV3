using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Items.Crafting;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting
{
    internal class CraftSecretFoundMessageComposer : ServerPacket
    {
        public bool isrecipe { get; }
        public int count { get; }
        public CraftSecretFoundMessageComposer(bool isrecipe, int count)
            : base(ServerPacketHeader.CraftSecretFoundMessageComposer)
        {
            this.isrecipe = isrecipe;
            this.count = count;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(count);
            packet.WriteBoolean(isrecipe);
        }
    }
}
