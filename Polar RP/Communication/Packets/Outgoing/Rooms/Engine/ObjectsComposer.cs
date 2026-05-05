using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using Polar.Utilities;
using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    class ObjectsComposer : ServerPacket
    {
        public ObjectsComposer(Item[] objects, Room room)
            : base(ServerPacketHeader.ObjectsMessageComposer)
        {
            bool hideWired = room.HideWired;
            var filteredItems = new List<Item>(objects.Length);
            var owners = new Dictionary<int, string>(objects.Length);

            foreach (var item in objects)
            {
                if (item == null) continue;
                if (hideWired && item.IsWired) continue;
                filteredItems.Add(item);
                if (!owners.ContainsKey(item.UserID))
                    owners[item.UserID] = item.Username;
            }

            WriteInteger(owners.Count);
            foreach (var owner in owners)
            {
                WriteInteger(owner.Key);
                WriteString(owner.Value);
            }

            WriteInteger(filteredItems.Count);
            foreach (var item in filteredItems)
                WriteFloorItem(item);
        }

        private void WriteFloorItem(Item item)
        {
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
        }
    }
}