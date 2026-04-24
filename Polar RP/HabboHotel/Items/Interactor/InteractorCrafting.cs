using System;
using System.Linq;
using Polar.HabboHotel.GameClients;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;
using MoreLinq;

namespace Polar.HabboHotel.Items.Interactor
{
    internal class InteractorCrafting : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {
            Item.ExtraData = "";
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            if (Session.GetRoomUser() == null)
                return;

            var User = Session.GetRoomUser();

            if (Rooms.Gamemap.TilesTouching(Item.GetX, Item.GetY, User.X, User.Y))
            {
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
            else
                User.MoveTo(Item.SquareInFront);
        }

        public void OnWiredTrigger(Item Item)
        {

        }
    }
}