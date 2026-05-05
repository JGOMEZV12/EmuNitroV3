using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.Utilities;
using System;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectUpdateComposer : ServerPacket
    {
        public Item Item { get; }
        public int UserId { get; }

        public ObjectUpdateComposer(Item item, string itemOwnerName)
            : base(ServerPacketHeader.ObjectUpdateMessageComposer)
        {
            Item = item;
            UserId = item.UserID;

            itemOwnerName ??= string.Empty;

            var interactionType = item.Data.InteractionType;
            string zStr = item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(GetStackHeight(item, interactionType, zStr));

            // ✅ stackHeight con AdjustableHeights
            WriteString(GetStackHeight(item, interactionType, zStr));

            if (interactionType == InteractionType.GIFT)
            {
                string[] parts = item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (parts.Length >= 7 &&
                    int.TryParse(parts[0], out int colorId) &&
                    int.TryParse(parts[6], out int ribbonId))
                    WriteInteger((colorId * 1000) + ribbonId);
                else
                    WriteInteger(1);
            }
            else if (interactionType == InteractionType.MUSIC_DISC)
                WriteInteger(int.TryParse(item.ExtraData, out int songId) ? songId : 1);
            else
                WriteInteger(1);

            try
            {
                ItemBehaviourUtility.GenerateExtradata(item, this);
            }
            catch (Exception ex)
            {
                Logging.WriteLine(
                    $"[GenerateExtradata FAIL] Item {item.Id} tipo {interactionType} ExtraData='{item.ExtraData}': {ex.Message}",
                    ConsoleColor.DarkGray);
                WriteInteger(0);
                WriteString(string.Empty);
            }

            WriteInteger(-1);

            if (interactionType == InteractionType.TELEPORT ||
                interactionType == InteractionType.SWITCH ||
                interactionType == InteractionType.VENDING_MACHINE ||
                interactionType == InteractionType.INFO_TERMINAL ||
                interactionType == InteractionType.POSTIT ||
                interactionType == InteractionType.PUZZLE_BOX)
                WriteInteger(2);
            else if (item.GetBaseItem().Modes > 1)
                WriteInteger(1);
            else
                WriteInteger(0);

            WriteInteger(item.UserID);
            WriteInteger(item.Data.Stackable ? 1 : 0);
            WriteInteger(item.Data.IsSeat ? 1 : 0);
            WriteInteger(0);
            WriteInteger(item.Data.Walkable ? 1 : 0);
            WriteInteger(item.Data.Width);
            WriteInteger(item.Data.Length);
            WriteInteger(0);
            WriteString(Convert.ToString(itemOwnerName));

            if (item.GetBaseItem().SpriteId < 0)
                WriteString(item.GetBaseItem().ItemName);
        }

        // ✅ Helper compartido — usado por ObjectsComposer y ObjectAddComposer
        internal static string GetStackHeight(Item item, InteractionType interactionType, string zStr)
        {
            if (interactionType == InteractionType.TROPHY ||
                interactionType == InteractionType.CRACKABLE ||
                item.GetBaseItem().ItemName == "gnome_box")
                return "1.0";

            // ✅ AdjustableHeights: Z + altura del estado actual
            if (item.GetBaseItem().AdjustableHeights != null &&
                item.GetBaseItem().AdjustableHeights.Count > 1 &&
                item.GetBaseItem().AdjustableHeights.TryGetValue(item.ExtraData, out double adjH))
            {
                double stackH = item.GetZ + adjH;
                return stackH.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (item.Data.Walkable || item.Data.IsSeat)
                return zStr;

            return string.Empty;
        }

        public ObjectUpdateComposer(Item item, int userId)
            : this(item, PolarEnvironment.GetUserInfoBy("username", "id", item.UserID.ToString()))
        { }
    }
}