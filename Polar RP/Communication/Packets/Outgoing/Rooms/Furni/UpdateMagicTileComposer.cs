using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni
{
    internal class UpdateMagicTileComposer : ServerPacket
    {
        public int ItemId { get; }
        public int Decimal { get; }

        public UpdateMagicTileComposer(int ItemId, int Decimal)
            : base(ServerPacketHeader.UpdateMagicTileMessageComposer)
        {
            this.ItemId = ItemId;
            this.Decimal = Decimal;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(ItemId);
            packet.WriteInteger(Decimal);
        }
    }
}
