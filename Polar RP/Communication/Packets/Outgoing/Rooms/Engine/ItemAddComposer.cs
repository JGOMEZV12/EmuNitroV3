using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ItemAddComposer : ServerPacket
    {
        public ItemAddComposer(Item Item)
            : base(ServerPacketHeader.ItemAddMessageComposer)
        {
            var interactionType = Item.Data.InteractionType;
            // ── serializeWallData — orden exacto del Java ─────────────────────
            WriteString(Item.Id.ToString());                                    // id (string)
            WriteInteger(Item.GetBaseItem().SpriteId);                         // spriteId
            WriteString(Item.wallCoord ?? string.Empty);                       // wallPosition

            // ✅ PostIt: solo la primera parte del extradata (color), igual que Java
            if (Item.GetBaseItem().InteractionType == InteractionType.POSTIT)
                WriteString(Item.ExtraData.Split(' ')[0]);
            else
                WriteString(Item.ExtraData ?? string.Empty);                   // extradata

            WriteInteger(-1);                                                   // secondsToExpiration
            // ── _usagePolicy ──────────────────────────────────────────────────
            if (interactionType == InteractionType.TELEPORT ||
                interactionType == InteractionType.SWITCH ||
                interactionType == InteractionType.VENDING_MACHINE ||
                interactionType == InteractionType.INFO_TERMINAL ||
                interactionType == InteractionType.POSTIT ||
                interactionType == InteractionType.PUZZLE_BOX)
                WriteInteger(2);
            else
                WriteInteger(Item.GetBaseItem().Modes > 1 ? 1 : 0);                                 // usagePolicy (isUsable)
            WriteInteger(Item.UserID);                                          // userId
            WriteInteger(Item.GetBaseItem().Stackable ? 1 : 0);             // allowStack
            WriteInteger(Item.GetBaseItem().IsSeat ? 1 : 0);             // allowSit
            WriteInteger(0);             // allowLay
            WriteInteger(Item.GetBaseItem().Walkable ? 1 : 0);             // allowWalk
            WriteInteger(Item.GetBaseItem().Width);                             // dimensionsX
            WriteInteger(Item.GetBaseItem().Length);                            // dimensionsY
            WriteInteger(0);                           // teleportTargetId

            // ✅ username — leído en FurnitureWallAddParser después del parse()
            WriteString(PolarEnvironment.GetUserInfoBy("username", "id", Item.UserID.ToString()));
        }
    }
}