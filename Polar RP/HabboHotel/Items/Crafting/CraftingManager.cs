using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboHotel.Items.Crafting
{
    class CraftingManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Items.Crafting");
        public static ConcurrentDictionary<string, CraftingRecipe> AllCraftingRecipes = new ConcurrentDictionary<string, CraftingRecipe>();
        public static ConcurrentDictionary<string, CraftingRecipe> SecretCraftingRecipes = new ConcurrentDictionary<string, CraftingRecipe>();
        public static ConcurrentDictionary<string, CraftingRecipe> NotSecretCraftingRecipes = new ConcurrentDictionary<string, CraftingRecipe>();
        public static List<string> CraftableItems = new List<string>();

        public static void Initialize()
        {
            AllCraftingRecipes.Clear();
            SecretCraftingRecipes.Clear();
            NotSecretCraftingRecipes.Clear();
            CraftableItems.Clear();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `crafting_recipes`");
                var recipes = dbClient.getTable();

                if (recipes != null)
                {
                    foreach (DataRow recipe in recipes.Rows)
                    {
                        string Items = recipe["items"].ToString().ToLower();
                        string Result = recipe["result"].ToString().ToLower();
                        bool Secret = PolarEnvironment.EnumToBool(recipe["secret"].ToString());

                        CraftingRecipe Recipe = new CraftingRecipe(Items, Result, Secret);

                        if (!AllCraftingRecipes.ContainsKey(Result))
                            AllCraftingRecipes.TryAdd(Result, Recipe);

                        if (!SecretCraftingRecipes.ContainsKey(Result) && Recipe.Secret)
                            SecretCraftingRecipes.TryAdd(Result, Recipe);

                        if (!NotSecretCraftingRecipes.ContainsKey(Result) && !Recipe.Secret)
                            NotSecretCraftingRecipes.TryAdd(Result, Recipe);
                    }
                }

                dbClient.SetQuery("SELECT * FROM `crafting_items`");
                var items = dbClient.getTable();

                if (items != null)
                {
                    foreach (DataRow item in items.Rows)
                    {
                        string ItemName = item["itemName"].ToString();

                        CraftableItems.Add(ItemName);
                    }
                }
            }
            //log.Info("Loaded " + SecretCraftingRecipes.Count + " secret and " + NotSecretCraftingRecipes.Count + " not secret crafting recipes.");
            Out.WriteLine("Cargado " + SecretCraftingRecipes.Count + " secretas y " + NotSecretCraftingRecipes.Count + " no secretas recetas crafting.", "Polar.HabboHotel", ConsoleColor.DarkGray);
        }

        public static CraftingRecipe getRecipe(string result)
        {
            if (AllCraftingRecipes.ContainsKey(result.ToLower()))
                return AllCraftingRecipes[result.ToLower()];
            else
                return null;
        }

        public static bool isCraftingItem(string item)
        {
            if (CraftableItems.Contains(item) || AllCraftingRecipes.ContainsKey(item))
                return true;

            return false;
        }
    }
}
