using System;
using System.Linq;
using System.Collections.Generic;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Utilities;
using Polar.HabboHotel.Groups;
using MoreLinq;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Crafting
{
    internal class CraftedRecipeExecutedMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int Id = Packet.PopInt();
            string ItemName = Packet.PopString();
            bool Successful = false;
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);

            var CraftingItem = CraftingManager.getRecipe(ItemName);

            if (CraftingItem != null)
                Successful = true;

            if (Successful)
            {
                foreach (var item in CraftingItem.ItemsNeeded)
                {
                    int AmountToRemove = item.Value;

                    for (int i = 0; i < AmountToRemove; i++)
                    {
                        foreach (var furni in Session.GetHabbo().GetInventoryComponent().GetItems)
                        {
                            if (furni.GetBaseItem().ItemName == item.Key)
                            {
                                if (CraftingItem.Result.ToLower() == "holorp_medipack" && (Gang == null || Gang.Id <= 1000))
                                    break;

                                using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                                    dbClient.RunQuery("DELETE FROM `items` WHERE `id` = '" + furni.Id + "' AND `user_id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                                Session.GetHabbo().GetInventoryComponent().RemoveItem(furni.Id);
                                break;
                            }
                        }
                    }
                }

                CryptoRandom Random = new CryptoRandom();
                int Chance = Random.Next(1, 101);
                int BonusChance = LevelManager.IntelligenceChance(Session);
                int BonusAmount = 0;

                bool Bonus = BonusChance >= Chance;

                if (Bonus)
                    BonusAmount = Random.Next(5, 26);

                if (CraftingItem.Result.ToLower() == "holorp_treasure" || CraftingItem.Result.ToLower() == "holorp_bullets" || CraftingItem.Result.ToLower() == "holorp_dynamite" || CraftingItem.Result.ToLower() == "holorp_medipack")
                {
                    if (CraftingItem.Result.ToLower() == "holorp_bullets")
                    {
                        Session.GetRoleplay().Bullets += 100 + BonusAmount;

                        if (Bonus)
                            Session.SendNotification("You have just crafted 100 bullets! [+" + BonusAmount + " bullets due to Intelligence Bonus]");
                        else
                            Session.SendNotification("You have just crafted 100 bullets!");
                    }
                    else if (CraftingItem.Result.ToLower() == "holorp_treasure")
                    {
                        Session.GetHabbo().Credits += 100 + BonusAmount;
                        Session.GetHabbo().UpdateCreditsBalance();

                        if (Bonus)
                            Session.SendNotification("You have just crafted $100 dollars! [+$" + BonusAmount + " due to Intelligence Bonus]");
                        else
                            Session.SendNotification("You have just crafted $100 dollars!");
                    }
                    else if (CraftingItem.Result.ToLower() == "holorp_dynamite")
                    {
                        if (Bonus)
                        {
                            Session.GetRoleplay().Dynamite += 2;
                            Session.SendNotification("¡Acabas de crear 1 barra de dinamita! [+1 Dynamite due to Intelligence Bonus]");
                        }
                        else
                        {
                            Session.GetRoleplay().Dynamite++;
                            Session.SendNotification("You have just crafted 1 stick of dynamite!");
                        }
                    }
                    else if (CraftingItem.Result.ToLower() == "holorp_medipack")
                    {
                        if (Gang == null)
                        {
                            Session.SendNotification("You are not part of any gang, so the medipack wasn't successfully crafted!");
                            return;
                        }

                        if (Gang.IsGang == false)
                        {
                            Session.SendNotification("You are not part of any gang, so the medipack wasn't successfully crafted!");
                            return;
                        }

                        if (Bonus)
                        {
                            Gang.MediPacks += 2;
                            Session.SendNotification("You have just crafted 1 medipack! [+1 medipack due to Intelligence Bonus]");
                        }
                        else
                        {
                            Gang.MediPacks++;
                            Session.SendNotification("You have just crafted 1 medipack!");
                        }

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            dbClient.RunQuery("UPDATE `rp_gangs` SET `medipacks` = '" + Gang.MediPacks + "' WHERE `id` = '" + Gang.Id + "'");
                    }
                }
                else
                {
                    ItemData Data = null;

                    foreach (var itemdata in PolarEnvironment.GetGame().GetItemManager()._items.Values)
                    {
                        if (itemdata.ItemName != CraftingItem.Result)
                            continue;

                        Data = itemdata;
                        break;
                    }

                    var Item = ItemFactory.CreateSingleItemNullable(Data, Session.GetHabbo(), "", "");

                    Session.GetHabbo().GetInventoryComponent().TryAddItem(Item);
                }
            }

            IEnumerable<Item> Items = Session.GetHabbo().GetInventoryComponent().GetWallAndFloor;

            Session.SendMessage(new CraftingExecutedMessageComposer(Successful, ItemName));
       
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
