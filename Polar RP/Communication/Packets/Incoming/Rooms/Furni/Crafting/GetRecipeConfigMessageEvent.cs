using System;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;
using MoreLinq;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Crafting
{
    internal class GetRecipeConfigMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string item = Packet.PopString();

            var CraftingItem = CraftingManager.getRecipe(item);

            if (CraftingItem == null)
            {
                Session.SendWhisper("This recipe could not be found! Please notify a developer!", 1);
                return;
            }

            IEnumerable<Item> Items = Session.GetHabbo().GetInventoryComponent().GetWallAndFloor;

            Session.GetRoleplay().CraftingCheck = false;
            int page = 0;
            int pages = ((Items.Count() - 1) / 700) + 1;

            if (!Items.Any())
            {
                Session.SendMessage(new FurniListComposer(Items.ToList(), 1, 0, Session.GetRoleplay().CraftingCheck));
            }
            else
            {
                foreach (ICollection<Item> batch in Items.Batch(700))
                {
                    Session.SendMessage(new FurniListComposer(batch.ToList(), pages, page, Session.GetRoleplay().CraftingCheck));

                    page++;
                }
            }

            Session.SendMessage(new CraftableProductsComposer(Session));

            Session.GetRoleplay().CraftingCheck = true;
            if (!Items.Any())
            {
                Session.SendMessage(new FurniListComposer(Items.ToList(), 1, 0, Session.GetRoleplay().CraftingCheck));
            }
            else
            {
                foreach (ICollection<Item> batch in Items.Batch(700))
                {
                    Session.SendMessage(new FurniListComposer(batch.ToList(), pages, page, Session.GetRoleplay().CraftingCheck));

                    page++;
                }
            }
        }
    }
}
