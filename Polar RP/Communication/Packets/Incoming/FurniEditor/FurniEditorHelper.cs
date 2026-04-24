using System;
using System.Collections.Generic;
using System.Data;

namespace Polar.Communication.Packets.Incoming.FurniEditor
{
    public static class FurniEditorHelper
    {
        public static readonly HashSet<string> AllowedUpdateFields = new HashSet<string>
        {
            "item_name", "public_name", "sprite_id", "type", "width", "length",
            "stack_height", "allow_stack", "allow_walk", "allow_sit", "allow_lay",
            "allow_gift", "allow_trade", "allow_recycle", "allow_marketplace_sell",
            "allow_inventory_stack", "interaction_type", "interaction_modes_count",
            "vending_ids", "customparams", "effect_id_male", "effect_id_female",
            "clothing_on_walk", "multiheight", "description"
        };

        public static readonly Dictionary<string, string> FieldMap = new Dictionary<string, string>
        {
            { "itemName",              "item_name" },
            { "publicName",            "public_name" },
            { "spriteId",              "sprite_id" },
            { "type",                  "type" },
            { "width",                 "width" },
            { "length",                "length" },
            { "stackHeight",           "stack_height" },
            { "allowStack",            "allow_stack" },
            { "allowWalk",             "allow_walk" },
            { "allowSit",              "allow_sit" },
            { "allowLay",              "allow_lay" },
            { "allowGift",             "allow_gift" },
            { "allowTrade",            "allow_trade" },
            { "allowRecycle",          "allow_recycle" },
            { "allowMarketplaceSell",  "allow_marketplace_sell" },
            { "allowInventoryStack",   "allow_inventory_stack" },
            { "interactionType",       "interaction_type" },
            { "interactionModesCount", "interaction_modes_count" },
            { "vendingIds",            "vending_ids" },
            { "customparams",          "customparams" },
            { "effectIdMale",          "effect_id_male" },
            { "effectIdFemale",        "effect_id_female" },
            { "clothingOnWalk",        "clothing_on_walk" },
            { "multiheight",           "multiheight" },
            { "description",           "description" }
        };

        public static Dictionary<string, object> ReadBaseItem(DataRow row)
        {
            return new Dictionary<string, object>
            {
                { "id",                      Convert.ToInt32(row["id"]) },
                { "sprite_id",               Convert.ToInt32(row["sprite_id"]) },
                { "item_name",               row["item_name"]?.ToString() ?? "" },
                { "public_name",             row["public_name"]?.ToString() ?? "" },
                { "type",                    row["type"]?.ToString() ?? "s" },
                { "width",                   Convert.ToInt32(row["width"]) },
                { "length",                  Convert.ToInt32(row["length"]) },
                { "stack_height",            Convert.ToDouble(row["stack_height"]) },
                { "allow_stack",             row["allow_stack"]?.ToString() ?? "1" },
                { "allow_walk",              row["allow_walk"]?.ToString() ?? "0" },
                { "allow_sit",               row["allow_sit"]?.ToString() ?? "0" },
                { "allow_lay",               row["allow_lay"]?.ToString() ?? "0" },
                { "interaction_type",        row["interaction_type"]?.ToString() ?? "" },
                { "interaction_modes_count", Convert.ToInt32(row["interaction_modes_count"]) }
            };
        }

        public static Dictionary<string, object> ReadFullItem(DataRow row)
        {
            var item = ReadBaseItem(row);

            item["allow_gift"] = row["allow_gift"]?.ToString() ?? "1";
            item["allow_trade"] = row["allow_trade"]?.ToString() ?? "1";
            item["allow_recycle"] = row["allow_recycle"]?.ToString() ?? "1";
            item["allow_marketplace_sell"] = row["allow_marketplace_sell"]?.ToString() ?? "1";
            item["allow_inventory_stack"] = row["allow_inventory_stack"]?.ToString() ?? "1";
            item["vending_ids"] = row["vending_ids"]?.ToString() ?? "";
            item["customparams"] = row["customparams"]?.ToString() ?? "";
            item["effect_id_male"] = Convert.ToInt32(row["effect_id_male"]);
            item["effect_id_female"] = Convert.ToInt32(row["effect_id_female"]);
            item["clothing_on_walk"] = row["clothing_on_walk"]?.ToString() ?? "";
            item["multiheight"] = row["multiheight"]?.ToString() ?? "";

            try { item["description"] = row["description"]?.ToString() ?? ""; }
            catch { item["description"] = ""; }

            return item;
        }

        public static Dictionary<string, object> ReadCatalogRef(DataRow row)
        {
            return new Dictionary<string, object>
            {
                { "id",           Convert.ToInt32(row["ci_id"]) },
                { "catalog_name", row["catalog_name"]?.ToString() ?? "" },
                { "cost_credits", Convert.ToInt32(row["cost_credits"]) },
                { "cost_points",  Convert.ToInt32(row["cost_points"]) },
                { "points_type",  Convert.ToInt32(row["points_type"]) },
                { "page_id",      Convert.ToInt32(row["ci_page_id"]) },
                { "page_caption", row["page_caption"]?.ToString() ?? "" }
            };
        }
    }
}