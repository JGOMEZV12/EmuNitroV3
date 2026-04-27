using Polar.HabboHotel.Items.Wired;
using System.Collections.Generic;
namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredEffectConfigComposer : ServerPacket
    {
        public WiredEffectConfigComposer(IWiredItem Box, List<int> BlockedItems)
            : base(ServerPacketHeader.WiredEffectConfigMessageComposer)
        {
            Box.Serialize(this);
            WriteInteger(BlockedItems?.Count ?? 0);
            if (BlockedItems != null) foreach (int Id in BlockedItems) WriteInteger(Id);
        }
    }
}