using Polar.HabboHotel.Items;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polar.Communication.Packets.Outgoing.Rooms.Engine
{
    class ObjectsComposer : ServerPacket
    {
        public ObjectsComposer(Item[] objects, Room room)
            : base(ServerPacketHeader.ObjectsMessageComposer)
        {
            bool hideWired = room.HideWired;
            var filteredItems = new List<Item>(objects.Length);
            var owners = new Dictionary<int, string>();

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
            {
                WriteFloorItem(item, item.UserID);
            }
        }

        private void WriteFloorItem(Item item, int userId)
        {
            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);
            WriteString(item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));

            var interactionType = item.Data.InteractionType;
            if (interactionType == InteractionType.TROPHY || interactionType == InteractionType.CRACKABLE || item.GetBaseItem().ItemName == "gnome_box")
                WriteString("1.0");
            else if (item.Data.Walkable || item.Data.IsSeat)
                WriteString(item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            else
                WriteString(string.Empty);

            if (interactionType == InteractionType.GIFT)
            {
                string[] giftData = item.ExtraData?.Split((char)5) ?? Array.Empty<string>();
                if (giftData.Length >= 7 && int.TryParse(giftData[6], out int giftStyle))
                    WriteInteger(giftStyle * 1000 + giftStyle);
                else
                    WriteInteger(1);
            }
            else if (interactionType == InteractionType.MUSIC_DISC)
            {
                if (int.TryParse(item.ExtraData, out int songId))
                    WriteInteger(songId);
                else
                    WriteInteger(1);
            }
            else
            {
                WriteInteger(1);
            }

            ItemBehaviourUtility.GenerateExtradata(item, this);

            WriteInteger(-1); // expires

            if (interactionType == InteractionType.TELEPORT || interactionType == InteractionType.SWITCH ||
                interactionType == InteractionType.VENDING_MACHINE || interactionType == InteractionType.INFO_TERMINAL ||
                interactionType == InteractionType.POSTIT)
                WriteInteger(2);
            else if (item.GetBaseItem().Modes > 1)
                WriteInteger(1);
            else
                WriteInteger(0);

            WriteInteger(userId);
        }
    }
}