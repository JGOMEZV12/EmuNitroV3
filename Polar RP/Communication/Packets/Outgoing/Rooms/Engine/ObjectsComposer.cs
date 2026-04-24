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

            // Una sola pasada: filtrar + recolectar owners simultáneamente
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
                WriteFloorItem(item, item.UserID);
        }

        private void WriteFloorItem(Item item, int userId)
        {
            WriteInteger(item.Id);
            WriteInteger(item.GetBaseItem().SpriteId);
            WriteInteger(item.GetX);
            WriteInteger(item.GetY);
            WriteInteger(item.Rotation);

            string zStr = item.GetZ.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            WriteString(zStr);

            var interactionType = item.Data.InteractionType;

            if (interactionType == InteractionType.TROPHY ||
                interactionType == InteractionType.CRACKABLE ||
                item.GetBaseItem().ItemName == "gnome_box")
                WriteString("1.0");
            else if (item.Data.Walkable || item.Data.IsSeat)
                WriteString(zStr); // reutiliza el string ya formateado
            else
                WriteString(string.Empty);

            // _extra
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

            // _data
            if (item.LimitedNo > 0)
            {
                WriteInteger(1);
                WriteInteger(256);
                WriteString(item.ExtraData ?? string.Empty);
                WriteInteger(item.LimitedNo);
                WriteInteger(item.LimitedTot);
            }
            else if (interactionType == InteractionType.INFO_TERMINAL)
            {
                WriteInteger(0);
                WriteInteger(1);
                WriteInteger(1);
                WriteString("internalLink");
                WriteString(item.ExtraData ?? string.Empty);
            }
            else if (interactionType == InteractionType.FX_PROVIDER)
            {
                WriteInteger(0);
                WriteInteger(1);
                WriteInteger(1);
                WriteString("effectId");
                WriteString(item.ExtraData ?? string.Empty);
            }
            else if (interactionType == InteractionType.PINATA)
            {
                WriteInteger(0);
                WriteInteger(7);
                WriteString("6");
                WriteInteger(string.IsNullOrEmpty(item.ExtraData) ? 0 : int.Parse(item.ExtraData));
                WriteInteger(100);
            }
            else if (interactionType == InteractionType.PINATATRIGGERED)
            {
                WriteInteger(0);
                WriteInteger(7);
                WriteString("0");
                WriteInteger(string.IsNullOrEmpty(item.ExtraData) ? 0 : int.Parse(item.ExtraData));
                WriteInteger(1);
            }
            else if (interactionType == InteractionType.MAGICEGG)
            {
                WriteInteger(0);
                WriteInteger(7);
                WriteString(item.ExtraData ?? string.Empty);
                WriteInteger(string.IsNullOrEmpty(item.ExtraData) ? 0 : int.Parse(item.ExtraData));
                WriteInteger(23);
            }
            else if (interactionType == InteractionType.MAGICCHEST)
            {
                WriteInteger(0);
                WriteInteger(7);
                WriteString(item.ExtraData ?? string.Empty);
                WriteInteger(string.IsNullOrEmpty(item.ExtraData) ? 0 : int.Parse(item.ExtraData));
                WriteInteger(1);
            }
            else
            {
                try
                {
                    ItemBehaviourUtility.GenerateExtradata(item, this);
                }
                catch (Exception ex)
                {
                    Logging.WriteLine($"[GenerateExtradata FAIL] Item {item.Id} tipo {interactionType} ExtraData='{item.ExtraData}': {ex.Message}", ConsoleColor.DarkGray);
                    WriteInteger(0);
                    WriteString(string.Empty);
                }
            }

            WriteInteger(-1); // _expires

            if (interactionType == InteractionType.TELEPORT ||
                interactionType == InteractionType.SWITCH ||
                interactionType == InteractionType.VENDING_MACHINE ||
                interactionType == InteractionType.INFO_TERMINAL ||
                interactionType == InteractionType.POSTIT)
                WriteInteger(2);
            else if (item.GetBaseItem().Modes > 1)
                WriteInteger(1);
            else
                WriteInteger(0);

            WriteInteger(userId);
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