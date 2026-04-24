using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Items.Crafting;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Crafting;
using Polar.Communication.Packets.Outgoing.Inventory.Furni;
using Polar.Utilities;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using MoreLinq;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Crafting
{
    internal class CraftSecretMessageEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int CraftingTable = Packet.PopInt();
            int Limit = Packet.PopInt();

            bool Successful = false;
            Group Gang = GroupManager.GetGang(Session.GetRoleplay().GangId);

            ConcurrentDictionary<string, int> PlacedItems = new ConcurrentDictionary<string, int>();

            for (var i = 0; i < Limit; i++)
            {
                int ItemId = Packet.PopInt();

                Item Item = Session.GetHabbo().GetInventoryComponent().GetItem(ItemId);

                if (Item == null)
                    continue;

                if (!PlacedItems.ContainsKey(Item.GetBaseItem().ItemName))
                    PlacedItems.TryAdd(Item.GetBaseItem().ItemName, 1);
                else
                    PlacedItems.TryUpdate(Item.GetBaseItem().ItemName, PlacedItems[Item.GetBaseItem().ItemName] + 1, PlacedItems[Item.GetBaseItem().ItemName]);
            }

            CraftingRecipe Recipe = null;

            foreach (var recipe in CraftingManager.AllCraftingRecipes.Values)
            {
                if (PlacedItems.OrderBy(r => r.Key).SequenceEqual(recipe.ItemsNeeded.OrderBy(r => r.Key)))
                {
                    Recipe = recipe;
                    break;
                }
            }

            if (Recipe != null)
                Successful = true;

            if (Successful)
            {
                foreach (var item in Recipe.ItemsNeeded)
                {
                    int AmountToRemove = item.Value;

                    for (int i = 0; i < AmountToRemove; i++)
                    {
                        foreach (var furni in Session.GetHabbo().GetInventoryComponent().GetItems)
                        {
                            if (furni.GetBaseItem().ItemName == item.Key)
                            {
                                if (Recipe.Result.ToLower() == "holorp_medipack" && (Gang == null || Gang.Id <= 1000))
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

                if (Recipe.Result.ToLower() == "holorp_treasure" || Recipe.Result.ToLower() == "holorp_bullets" || Recipe.Result.ToLower() == "holorp_dynamite" || Recipe.Result.ToLower() == "holorp_medipack")
                {
                    if (Recipe.Result.ToLower() == "holorp_bullets")
                    {
                        Session.GetRoleplay().Bullets += 100 + BonusAmount;

                        if (Bonus)
                            Session.SendNotification("Acabas de crear 100 balas! [+" + BonusAmount + " balas debido a la bonificación de inteligencia]");
                        else
                            Session.SendNotification("acabas de hacer 100 balas");
                    }
                    else if (Recipe.Result.ToLower() == "holorp_treasure")
                    {
                        Session.GetHabbo().Credits += 100 + BonusAmount;
                        Session.GetHabbo().UpdateCreditsBalance();

                        if (Bonus)
                            Session.SendNotification("¡Acabas de hacer $ 100 dólares! [+$" + BonusAmount + " por inteligencia]");
                        else
                            Session.SendNotification("¡Acabas de hacer $ 100 dólares!");
                    }
                    else if (Recipe.Result.ToLower() == "holorp_dynamite")
                    {
                        if (Bonus)
                        {
                            Session.GetRoleplay().Dynamite += 2;
                            Session.SendNotification("¡Acabas de crear 1 barra de dinamita! [+1 Dynamite due to Intelligence Bonus]");
                        }
                        else
                        {
                            Session.GetRoleplay().Dynamite++;
                            Session.SendNotification("¡Acabas de crear 1 barra de dinamita!");
                        }
                    }
                    else if (Recipe.Result.ToLower() == "holorp_medipack")
                    {
                        if (Gang == null)
                        {
                            Session.SendNotification("¡Usted no es parte de ninguna pandilla, por lo que el botiquín no fue creado con éxito!");
                            return;
                        }

                        if (Gang.IsGang == false)
                        {
                            Session.SendNotification("¡Usted no es parte de ninguna pandilla, por lo que el botiquín no fue creado con éxito!");
                            return;
                        }

                        if (Bonus)
                        {
                            Gang.MediPacks += 2;
                            Session.SendNotification("¡Acabas de crear 1 medipack! [+1 medipack due to Intelligence Bonus]");
                        }
                        else
                        {
                            Gang.MediPacks++;
                            Session.SendNotification("¡Acabas de crear 1 medipack!");
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
                        if (itemdata.ItemName != Recipe.Result)
                            continue;

                        Data = itemdata;
                        break;
                    }

                    var Item = ItemFactory.CreateSingleItemNullable(Data, Session.GetHabbo(), "", "");

                    Session.GetHabbo().GetInventoryComponent().TryAddItem(Item);
                }
                if (Recipe.Secret)
                {
                    if (!Session.GetHabbo().UnlockedRecipes.Contains(Recipe))
                    {
                        Session.GetHabbo().UnlockedRecipes.Add(Recipe);

                        using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                            dbClient.RunQuery("INSERT INTO `user_recipes` (`user_id`,`recipe`) VALUES ('" + Session.GetHabbo().Id + "','" + Recipe.Result + "')");
                    }
                }
            }

            Session.SendMessage(new CraftingExecutedMessageComposer(Successful, Recipe != null ? Recipe.Result : "recipe_doesnt_exist"));

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
