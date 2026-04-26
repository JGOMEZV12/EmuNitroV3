using System;
using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListComposer : ServerPacket
    {
        public FurniListComposer(ICollection<Item> Items, int pages, int page, bool CraftingCheck)
            : base(ServerPacketHeader.FurniListMessageComposer)
        {
            WriteInteger(pages);
            WriteInteger(page);

            List<Item> itemList = Items.ToList();
            List<Item> filteredItems = new List<Item>();

            if (CraftingCheck)
            {
                foreach (var item in itemList)
                {
                    if (!CraftingManager.isCraftingItem(item.GetBaseItem().ItemName))
                        filteredItems.Add(item);
                }
            }
            else
            {
                filteredItems = itemList;
            }

            WriteInteger(filteredItems.Count);
            foreach (Item item in filteredItems)
            {
                ItemBehaviourUtility.WriteInventoryItem(item, this);
            }
        }
    }
}