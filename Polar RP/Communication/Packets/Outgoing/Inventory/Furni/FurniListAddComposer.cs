using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListAddComposer : ServerPacket
    {
        public FurniListAddComposer(Item item)
            : base(ServerPacketHeader.FurniListAddMessageComposer)
        {
            ItemBehaviourUtility.WriteInventoryItem(item, this);
            WriteInteger(100); // Standard footer
        }
    }
}