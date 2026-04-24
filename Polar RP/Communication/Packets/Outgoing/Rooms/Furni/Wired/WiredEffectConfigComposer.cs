
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Wired;


namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredEffectConfigComposer : ServerPacket
    {
        public WiredEffectConfigComposer(IWiredItem Box, List<int> BlockedItems)
            : base(ServerPacketHeader.WiredEffectConfigMessageComposer)
        {
            Box.Serialize(this);

            base.WriteInteger(BlockedItems.Count()); // Incompatible items loop
            if (BlockedItems.Count() > 0)
            {
                foreach (int ItemId in BlockedItems.ToList())
                    base.WriteInteger(ItemId);
            }
        }
    }
}