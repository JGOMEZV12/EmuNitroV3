using System;
using System.Collections.Generic;
using System.Data;
using Polar.Database.Interfaces;

namespace Polar.Core
{
    public static class DatabaseCompatibility
    {
        public static string FurnitureTable { get; private set; } = "furniture";
        public static string CatalogItemsTable { get; private set; } = "catalog_items";
        public static string CatalogPagesTable { get; private set; } = "catalog_pages";

        // Mappings for Furniture/ItemsBase
        public static string FurniIdColumn { get; private set; } = "id";
        public static string FurniSpriteIdColumn { get; private set; } = "sprite_id";
        public static string FurniItemNameColumn { get; private set; } = "item_name";
        public static string FurniTypeColumn { get; private set; } = "type";
        public static string FurniWidthColumn { get; private set; } = "width";
        public static string FurniLengthColumn { get; private set; } = "length";
        public static string FurniStackHeightColumn { get; private set; } = "stack_height";
        public static string FurniAllowStackColumn { get; private set; } = "can_stack";
        public static string FurniAllowSitColumn { get; private set; } = "can_sit";
        public static string FurniAllowLayColumn { get; private set; } = "can_lay";
        public static string FurniAllowWalkColumn { get; private set; } = "is_walkable";
        public static string FurniAllowGiftColumn { get; private set; } = "allow_gift";
        public static string FurniAllowTradeColumn { get; private set; } = "allow_trade";
        public static string FurniAllowRecycleColumn { get; private set; } = "allow_recycle";
        public static string FurniAllowMarketplaceSellColumn { get; private set; } = "allow_marketplace_sell";
        public static string FurniAllowInventoryStackColumn { get; private set; } = "allow_inventory_stack";
        public static string FurniInteractionTypeColumn { get; private set; } = "interaction_type";
        public static string FurniInteractionModesCountColumn { get; private set; } = "interaction_modes_count";
        public static string FurniVendingIdsColumn { get; private set; } = "vending_ids";
        public static string FurniHeightAdjustableColumn { get; private set; } = "height_adjustable";
        public static string FurniEffectIdColumn { get; private set; } = "effect_id";
        public static string FurniIsRareColumn { get; private set; } = "is_rare";
        public static string FurniClothingIdColumn { get; private set; } = "clothing_id";
        public static string FurniExtraRotColumn { get; private set; } = "extra_rot";
        // Mappings for Catalog
        public static string CatalogItemIdColumn { get; private set; } = "id";
        public static string CatalogItemBaseIdColumn { get; private set; } = "item_id";
        public static string CatalogItemCostDucketsColumn { get; private set; } = "cost_pixels";
        public static string CatalogItemCostDiamondsColumn { get; private set; } = "cost_diamonds";
        public static string CatalogPageLayoutColumn { get; private set; } = "page_layout";
        public static string CatalogPageStrings1Column { get; private set; } = "page_strings_1";
        public static string CatalogPageStrings2Column { get; private set; } = "page_strings_2";
        public static string CatalogPageStrings3Column { get; private set; } = "page_text_details";
        public static string CatalogPageText1Column { get; private set; } = "page_text_1";
        public static string CatalogPageText2Column { get; private set; } = "page_text_2";
        public static string CatalogPageText3Column { get; private set; } = "page_text_teaser";
        public static string CatalogPageEnabledColumn { get; private set; } = "enabled";
        public static string CatalogPageVisibleColumn { get; private set; } = "visible";
        public static string CatalogItemOfferActiveColumn { get; private set; } = "offer_active";
        public static string CatalogBotPresetsTable { get; private set; } = "catalog_bot_presets";
        public static string CatalogClothingTable { get; private set; } = "catalog_clothing";
        public static string ItemsTable { get; private set; } = "items";
        public static string ItemsBaseItemColumn { get; private set; } = "base_item";
        public static string ItemsGroupIdColumn { get; private set; } = "group_id";
        public static string MarketplaceDataTable { get; private set; } = "catalog_marketplace_data";

        public static void Initialize()
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                // Detect Furniture table
                dbClient.SetQuery("SHOW TABLES LIKE 'items_base'");
                if (dbClient.findsResult())
                {
                    FurnitureTable = "items_base";
                }
                else
                {
                    dbClient.SetQuery("SHOW TABLES LIKE 'furniture'");
                    if (dbClient.findsResult())
                        FurnitureTable = "furniture";
                }

                // Determine internal ItemName column (classname in Arcturus, item_name in Plus)
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'classname'");
                if (dbClient.findsResult())
                    FurniItemNameColumn = "classname";
                else
                {
                    dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'item_name'");
                    if (dbClient.findsResult())
                        FurniItemNameColumn = "item_name";
                }

                // Detect SpriteId
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'sprite_id'");
                if (dbClient.findsResult())
                    FurniSpriteIdColumn = "sprite_id";
                else
                    FurniSpriteIdColumn = "id";

                // Detect StackHeight
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'stack_height'");
                if (dbClient.findsResult())
                    FurniStackHeightColumn = "stack_height";
                else
                    FurniStackHeightColumn = "height";

                // Detect AllowStack
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_stack'");
                if (dbClient.findsResult())
                    FurniAllowStackColumn = "allow_stack";
                else
                    FurniAllowStackColumn = "can_stack";

                // Detect AllowSit
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_sit'");
                if (dbClient.findsResult())
                    FurniAllowSitColumn = "allow_sit";
                else
                    FurniAllowSitColumn = "can_sit";

                // Detect AllowLay
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_lay'");
                if (dbClient.findsResult())
                    FurniAllowLayColumn = "allow_lay";
                else
                    FurniAllowLayColumn = "can_lay";

                // Detect AllowWalk
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_walk'");
                if (dbClient.findsResult())
                    FurniAllowWalkColumn = "allow_walk";
                else
                    FurniAllowWalkColumn = "is_walkable";

                // Detect HeightAdjustable / Multiheight
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'variable_heights'");
                if (dbClient.findsResult())
                    FurniHeightAdjustableColumn = "variable_heights";
                else
                {
                    dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'multiheight'");
                    if (dbClient.findsResult())
                        FurniHeightAdjustableColumn = "multiheight";
                    else
                        FurniHeightAdjustableColumn = "height_adjustable";
                }

                // Detect IsRare / Rare
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'rare'");
                if (dbClient.findsResult())
                    FurniIsRareColumn = "rare";
                else
                    FurniIsRareColumn = "is_rare";

                // Detect InteractionModesCount
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'interaction_modes_count'");
                if (dbClient.findsResult())
                    FurniInteractionModesCountColumn = "interaction_modes_count";
                else
                    FurniInteractionModesCountColumn = "interaction_modes_count";

                // Detect VendingIds
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'vending_ids'");
                if (dbClient.findsResult())
                    FurniVendingIdsColumn = "vending_ids";
                else
                    FurniVendingIdsColumn = "vending_ids";

                // Detect AllowGift
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_gift'");
                if (dbClient.findsResult())
                    FurniAllowGiftColumn = "allow_gift";

                // Detect AllowTrade
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_trade'");
                if (dbClient.findsResult())
                    FurniAllowTradeColumn = "allow_trade";

                // Detect AllowRecycle
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_recycle'");
                if (dbClient.findsResult())
                    FurniAllowRecycleColumn = "allow_recycle";

                // Detect AllowMarketplaceSell
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_marketplace_sell'");
                if (dbClient.findsResult())
                    FurniAllowMarketplaceSellColumn = "allow_marketplace_sell";

                // Detect AllowInventoryStack
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'allow_inventory_stack'");
                if (dbClient.findsResult())
                    FurniAllowInventoryStackColumn = "allow_inventory_stack";

                // Detect InteractionType
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'interaction_type'");
                if (dbClient.findsResult())
                    FurniInteractionTypeColumn = "interaction_type";

                // Detect EffectId
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'effect_id'");
                if (dbClient.findsResult())
                    FurniEffectIdColumn = "effect_id";

                // Detect ClothingId
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'clothing_id'");
                if (dbClient.findsResult())
                    FurniClothingIdColumn = "clothing_id";

                // Detect ExtraRot
                dbClient.SetQuery($"SHOW COLUMNS FROM `{FurnitureTable}` LIKE 'extra_rot'");
                if (dbClient.findsResult())
                    FurniExtraRotColumn = "extra_rot";

                // Confirm standard columns
                FurniIdColumn = "id";
                FurniTypeColumn = "type";
                FurniWidthColumn = "width";
                FurniLengthColumn = "length";

                // Detect Catalog Items Columns
                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_items` LIKE 'cost_duckets'");
                if (dbClient.findsResult())
                    CatalogItemCostDucketsColumn = "cost_duckets";
                
                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_items` LIKE 'cost_points'");
                if (dbClient.findsResult())
                    CatalogItemCostDiamondsColumn = "cost_points";

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_items` LIKE 'item_ids'");
                if (dbClient.findsResult())
                    CatalogItemBaseIdColumn = "item_ids";

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_items` LIKE 'offer_active'");
                if (dbClient.findsResult())
                    CatalogItemOfferActiveColumn = "offer_active";
                else
                    CatalogItemOfferActiveColumn = "1"; // Hack for query if column doesn't exist

                // Detect Catalog Pages Columns
                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'layout'");
                if (dbClient.findsResult())
                    CatalogPageLayoutColumn = "layout";

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_headline'");
                if (dbClient.findsResult())
                    CatalogPageStrings1Column = "page_headline";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_strings_1'");
                    if (dbClient.findsResult())
                        CatalogPageStrings1Column = "page_strings_1";
                }

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_teaser'");
                if (dbClient.findsResult())
                    CatalogPageStrings2Column = "page_teaser";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_strings_2'");
                    if (dbClient.findsResult())
                        CatalogPageStrings2Column = "page_strings_2";
                }

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_text_details'");
                if (dbClient.findsResult())
                    CatalogPageStrings3Column = "page_text_details";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'page_strings_2'");
                    if (dbClient.findsResult())
                        CatalogPageStrings3Column = "page_strings_2";
                }

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'enabled'");
                if (dbClient.findsResult())
                    CatalogPageEnabledColumn = "enabled";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'is_enabled'");
                    if (dbClient.findsResult())
                        CatalogPageEnabledColumn = "is_enabled";
                }

                dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'visible'");
                if (dbClient.findsResult())
                    CatalogPageVisibleColumn = "visible";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `catalog_pages` LIKE 'is_visible'");
                    if (dbClient.findsResult())
                        CatalogPageVisibleColumn = "is_visible";
                }

                // Detect Clothing Table
                dbClient.SetQuery("SHOW TABLES LIKE 'catalog_clothing'");
                if (dbClient.findsResult())
                    CatalogClothingTable = "catalog_clothing";
                else
                {
                    dbClient.SetQuery("SHOW TABLES LIKE 'clothing'");
                    if (dbClient.findsResult())
                        CatalogClothingTable = "clothing";
                }

                // Detect Bot Presets Table
                dbClient.SetQuery("SHOW TABLES LIKE 'catalog_bot_presets'");
                if (dbClient.findsResult())
                    CatalogBotPresetsTable = "catalog_bot_presets";
                else
                {
                    dbClient.SetQuery("SHOW TABLES LIKE 'catalog_bots'");
                    if (dbClient.findsResult())
                        CatalogBotPresetsTable = "catalog_bots";
                }

                // Detect Items Table Columns
                dbClient.SetQuery("SHOW COLUMNS FROM `items` LIKE 'base_item'");
                if (dbClient.findsResult())
                    ItemsBaseItemColumn = "base_item";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `items` LIKE 'item_id'");
                    if (dbClient.findsResult())
                        ItemsBaseItemColumn = "item_id";
                }

                dbClient.SetQuery("SHOW COLUMNS FROM `items` LIKE 'group_id'");
                if (dbClient.findsResult())
                    ItemsGroupIdColumn = "group_id";
                else
                {
                    dbClient.SetQuery("SHOW COLUMNS FROM `items` LIKE 'guild_id'");
                    if (dbClient.findsResult())
                        ItemsGroupIdColumn = "guild_id";
                }

                // Marketplace data table
                dbClient.SetQuery("SHOW TABLES LIKE 'catalog_marketplace_data'");
                if (dbClient.findsResult())
                    MarketplaceDataTable = "catalog_marketplace_data";
                else
                {
                    dbClient.SetQuery("SHOW TABLES LIKE 'catalog_marketplace_items'");
                    if (dbClient.findsResult())
                        MarketplaceDataTable = "catalog_marketplace_items";
                }
            }
        }

        public static bool ColumnExists(string table, string column)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery($"SHOW COLUMNS FROM `{table}` LIKE @column");
                dbClient.AddParameter("column", column);
                return dbClient.findsResult();
            }
        }
    }
}
