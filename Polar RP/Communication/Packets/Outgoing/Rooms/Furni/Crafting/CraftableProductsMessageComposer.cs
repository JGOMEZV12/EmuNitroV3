using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Items.Crafting;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting
{
    internal class CraftableProductsComposer : ServerPacket
    {
        public GameClient Session { get; }
        public CraftableProductsComposer(GameClient Session)
            : base(ServerPacketHeader.CraftableProductsMessageComposer)
        {
            this.Session = Session;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(CraftingManager.NotSecretCraftingRecipes.Count + Session.GetHabbo().UnlockedRecipes.Count);

            foreach (var recipe in CraftingManager.NotSecretCraftingRecipes.Values)
            {
                packet.WriteString(recipe.Result);
                packet.WriteString(recipe.Result);
            }

            foreach (var recipe in Session.GetHabbo().UnlockedRecipes)
            {
                packet.WriteString(recipe.Result);
                packet.WriteString(recipe.Result);
            }

            packet.WriteInteger(CraftingManager.CraftableItems.Count);

            foreach (var itemName in CraftingManager.CraftableItems)
            {
                packet.WriteString(itemName);
            }
        }
    }
}
