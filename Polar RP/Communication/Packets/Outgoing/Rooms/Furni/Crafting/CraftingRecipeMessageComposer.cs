using System;
using Polar.HabboHotel.Items.Crafting;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting
{
    internal class CraftingRecipeMessageComposer : ServerPacket
    {
        public CraftingRecipe Recipe { get; }
        public CraftingRecipeMessageComposer (CraftingRecipe recipe) : base(ServerPacketHeader.CraftingRecipeMessageComposer)
        {
            this.Recipe = recipe;
            Compose(this);
        }
        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Recipe.ItemsNeeded.Count); // Count of different items

            foreach (var item in Recipe.ItemsNeeded)
            {
                packet.WriteInteger(item.Value); // How many of the item
                packet.WriteString(item.Key); // Item name
            }
        }
    }
}
