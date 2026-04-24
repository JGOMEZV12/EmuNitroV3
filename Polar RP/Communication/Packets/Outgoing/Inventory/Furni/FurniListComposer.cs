using System;
using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.Catalog.Utilities;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListComposer : ServerPacket
    {
        public int CraftableItemCount = 0;

        public FurniListComposer(ICollection<Item> Items, int pages, int page, bool CraftingCheck)
            : base(ServerPacketHeader.FurniListMessageComposer)
        {
            WriteInteger(pages);
            WriteInteger(page);

            List<Item> itemList = Items.ToList();
            List<Item> filteredItems;

            if (CraftingCheck)
            {
                filteredItems = new List<Item>();
                foreach (var item in itemList)
                {
                    if (CraftingManager.isCraftingItem(item.GetBaseItem().ItemName))
                        CraftableItemCount++;
                    else
                        filteredItems.Add(item);
                }
            }
            else
            {
                filteredItems = itemList;
            }

            WriteInteger(filteredItems.Count);

            foreach (Item item in filteredItems)
                WriteItem(item);
        }

        private void WriteItem(Item item)
        {
            string itemName = item.GetBaseItem().ItemName;

            // Java campo 1: giftAdjustedId
            // Reemplazar por item.GiftAdjustedId si tu modelo lo expone
            WriteInteger(item.Id);

            // Java campo 2: type.code (e.g. "s" floor, "i" wall)
            WriteString(item.GetBaseItem().Type.ToString().ToUpper());

            // Java campo 3: ID real del item
            WriteInteger(item.Id);

            // Java campo 4: spriteId
            WriteInteger(item.GetBaseItem().SpriteId);

            // ---- Extradata (lógica exacta del Java) ----
            bool isSpecialName = itemName == "floor" ||
                                 itemName == "landscape" ||
                                 itemName == "song_disk" ||
                                 itemName == "wallpaper" ||
                                 itemName == "poster";

            if (isSpecialName)
            {
                // Rama 1: items especiales por nombre
                // Java: escribe int categórico + addExtraDataToResponse(int 0 + string)
                switch (itemName)
                {
                    case "landscape": WriteInteger(4); break;
                    case "floor": WriteInteger(3); break;
                    case "wallpaper": WriteInteger(2); break;
                    case "poster": WriteInteger(6); break;
                    case "song_disk": WriteInteger(8); break;
                }
                WriteInteger(0);
                WriteString(item.ExtraData ?? string.Empty);
            }
            else
            {
                // Rama 2: resto de items
                // Java: gnome_box=13, gifts=colorId*1000+ribbonId, resto=1
                // Luego serializeExtradata (= GenerateExtradata en C#)
                if (itemName == "gnome_box")
                    WriteInteger(13);
                else if (item.GetBaseItem().InteractionType == InteractionType.GIFT)
                    WriteInteger(GetGiftStyleInt(item));
                else
                    WriteInteger(1);

                ItemBehaviourUtility.GenerateExtradata(item, this);
            }
            // --------------------------------------------

            // Flags comunes
            WriteBoolean(item.GetBaseItem().AllowEcotronRecycle);
            WriteBoolean(item.GetBaseItem().AllowTrade);
            // Java: !isLimited() && allowInventoryStack()
            WriteBoolean(item.LimitedNo == 0 && item.GetBaseItem().AllowInventoryStack);
            // Java: allowMarketplace() — corregido, antes era IsRare()
            WriteBoolean(item.GetBaseItem().AllowMarketplaceSell);
            WriteInteger(-1);   // segundos para expiración
            WriteBoolean(true);
            WriteInteger(-1);   // RoomId del item

            // Footer exclusivo de floor items (Java: FurnitureType.FLOOR)
            if (!item.IsWallItem)
            {
                WriteString(string.Empty);

                if (itemName == "song_disk")
                {
                    // Java: última línea del extradata = track ID, luego return
                    WriteInteger(GetSongDiskTrackId(item));
                    return;
                }

                // Java: gifts → colorId*1000+ribbonId; resto → 1
                if (item.GetBaseItem().InteractionType == InteractionType.GIFT)
                    WriteInteger(GetGiftStyleInt(item));
                else
                    WriteInteger(1);
            }
        }

        /// <summary>
        /// Java: (colorId * 1000) + ribbonId para InteractionGift.
        /// Ajustar si tu modelo expone los campos directamente.
        /// </summary>
        private int GetGiftStyleInt(Item item)
        {
            string[] parts = item.ExtraData?.Split(Convert.ToChar(5)) ?? Array.Empty<string>();
            if (parts.Length >= 7 &&
                int.TryParse(parts[0], out int colorId) &&
                int.TryParse(parts[6], out int ribbonId))
            {
                return (colorId * 1000) + ribbonId;
            }
            return 1;
        }

        /// <summary>
        /// Java: última línea del extradata del song_disk = track ID.
        /// </summary>
        private int GetSongDiskTrackId(Item item)
        {
            if (string.IsNullOrEmpty(item.ExtraData)) return 0;
            string[] lines = item.ExtraData.Split('\n');
            return int.TryParse(lines[lines.Length - 1], out int id) ? id : 0;
        }
    }
}