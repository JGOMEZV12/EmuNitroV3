using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Utilities;
using System;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    internal class ObjectAddComposer : ServerPacket
    {
        public ObjectAddComposer(Item item, string itemOwnerName)
            : base(ServerPacketHeader.ObjectAddMessageComposer)
        {
            itemOwnerName ??= string.Empty;

            var interactionType = item.Data.InteractionType;
            string zStr = item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(ObjectUpdateComposer.GetStackHeight(item, interactionType, zStr));

            // ✅ stackHeight con AdjustableHeights
            WriteString(ObjectUpdateComposer.GetStackHeight(item, interactionType, zStr));

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
            WriteInteger((item.GetBaseItem().Modes > 1) ? 1 : 0);
            WriteInteger(item.UserID);
            WriteInteger(item.Data.Stackable ? 1 : 0);
            WriteInteger(item.Data.IsSeat ? 1 : 0);
            WriteInteger(0);
            WriteInteger(item.Data.Walkable ? 1 : 0);
            WriteInteger(item.Data.Width);
            WriteInteger(item.Data.Length);
            WriteInteger(0);
            WriteString(Convert.ToString(itemOwnerName));
        }

        public ObjectAddComposer(Item item, Room room)
            : this(item, PolarEnvironment.GetUserInfoBy("username", "id", item.UserID.ToString()))
        { }
    }
}