using System;
using System.Data;
using System.Collections.Generic;

using log4net;
using Polar.Core;

using Polar.Database.Interfaces;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Text;

namespace Polar.HabboHotel.Items
{
    public class ItemDataManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Items.ItemDataManager");

        public Dictionary<int, ItemData> _items;
        public Dictionary<int, ItemData> _gifts;//<SpriteId, Item>

        public ItemDataManager()
        {
            this._items = new Dictionary<int, ItemData>();
            this._gifts = new Dictionary<int, ItemData>();
        }

        public async Task InitAsync()
        {
            if (this._items.Count > 0)
                this._items.Clear();

            await Task.Run(() =>
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery($"SELECT * FROM `{DatabaseCompatibility.FurnitureTable}`");
                    DataTable ItemData = dbClient.getTable();

                    if (ItemData != null)
                    {
                        foreach (DataRow Row in ItemData.Rows)
                        {
                            try
                            {
                                int id = SafeToInt(Row[DatabaseCompatibility.FurniIdColumn]);
                                int spriteID = SafeToInt(Row[DatabaseCompatibility.FurniSpriteIdColumn]);
                                string itemName = Convert.ToString(Row[DatabaseCompatibility.FurniItemNameColumn]) ?? "";
                                string publicname = Convert.ToString(Row.Table.Columns.Contains("public_name") ? Row["public_name"] : itemName) ?? "";
                                string type = Row[DatabaseCompatibility.FurniTypeColumn].ToString();
                                int width = Convert.ToInt32(Row.Table.Columns.Contains(DatabaseCompatibility.FurniWidthColumn) ? Row[DatabaseCompatibility.FurniWidthColumn] : 1);
                                int length = Convert.ToInt32(Row.Table.Columns.Contains(DatabaseCompatibility.FurniLengthColumn) ? Row[DatabaseCompatibility.FurniLengthColumn] : 1);
                                double height = Convert.ToDouble(Row[DatabaseCompatibility.FurniStackHeightColumn]);

                                bool allowStack = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowStackColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowStackColumn].ToString()) : true;
                                bool allowWalk = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowWalkColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowWalkColumn].ToString()) : true;
                                bool allowSit = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowSitColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowSitColumn].ToString()) : false;
                                bool allowRecycle = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowRecycleColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowRecycleColumn].ToString()) : true;
                                bool allowTrade = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowTradeColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowTradeColumn].ToString()) : true;
                                bool allowMarketplace = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowMarketplaceSellColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowMarketplaceSellColumn].ToString()) : true;
                                bool allowGift = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowGiftColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowGiftColumn].ToString()) : true;
                                bool allowInventoryStack = Row.Table.Columns.Contains(DatabaseCompatibility.FurniAllowInventoryStackColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniAllowInventoryStackColumn].ToString()) : true;

                                InteractionType interactionType = InteractionType.NONE;
                                string rawInteraction = "";
                                if (Row.Table.Columns.Contains(DatabaseCompatibility.FurniInteractionTypeColumn))
                                {
                                    rawInteraction = Convert.ToString(Row[DatabaseCompatibility.FurniInteractionTypeColumn]);
                                    interactionType = InteractionTypes.GetTypeFromString(rawInteraction);
                                }

                                int behaviourData = SafeToInt(Row.Table.Columns.Contains("behaviour_data") ? Row["behaviour_data"] : 0);
                                int cycleCount = Row.Table.Columns.Contains(DatabaseCompatibility.FurniInteractionModesCountColumn) ? Convert.ToInt32(Row[DatabaseCompatibility.FurniInteractionModesCountColumn]) : 1;
                                string vendingIDS = Row.Table.Columns.Contains(DatabaseCompatibility.FurniVendingIdsColumn) ? Convert.ToString(Row[DatabaseCompatibility.FurniVendingIdsColumn]) : "";

                                List<double> heightAdjustable = new List<double>();
                                if (Row.Table.Columns.Contains(DatabaseCompatibility.FurniHeightAdjustableColumn) &&
                                    !string.IsNullOrEmpty(Row[DatabaseCompatibility.FurniHeightAdjustableColumn].ToString()))
                                {
                                    foreach (string val in Row[DatabaseCompatibility.FurniHeightAdjustableColumn].ToString().Split(','))
                                    {
                                        // ✅ InvariantCulture para que "0.5" parsee correctamente en cualquier sistema
                                        if (double.TryParse(val.Trim(), System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture, out double h))
                                            heightAdjustable.Add(h);
                                    }
                                }
                                int EffectId = Row.Table.Columns.Contains(DatabaseCompatibility.FurniEffectIdColumn) ? Convert.ToInt32(Row[DatabaseCompatibility.FurniEffectIdColumn]) : 0;
                                bool IsRare = Row.Table.Columns.Contains(DatabaseCompatibility.FurniIsRareColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniIsRareColumn].ToString()) : false;
                                int ClothingId = Row.Table.Columns.Contains(DatabaseCompatibility.FurniClothingIdColumn) ? Convert.ToInt32(Row[DatabaseCompatibility.FurniClothingIdColumn]) : 0;
                                bool ExtraRot = Row.Table.Columns.Contains(DatabaseCompatibility.FurniExtraRotColumn) ? PolarEnvironment.EnumToBool(Row[DatabaseCompatibility.FurniExtraRotColumn].ToString()) : false; 
                                
                                if (!this._gifts.ContainsKey(spriteID))
                                    this._gifts.Add(spriteID, new ItemData(id, spriteID, itemName, publicname, type, width, length, height, allowStack, allowWalk, allowSit, allowRecycle, allowTrade, allowMarketplace, allowGift, allowInventoryStack, interactionType, behaviourData, cycleCount, vendingIDS, heightAdjustable, EffectId, IsRare, ClothingId, ExtraRot, rawInteraction));

                                if (!this._items.ContainsKey(id))
                                    this._items.Add(id, new ItemData(id, spriteID, itemName, publicname, type, width, length, height, allowStack, allowWalk, allowSit, allowRecycle, allowTrade, allowMarketplace, allowGift, allowInventoryStack, interactionType, behaviourData, cycleCount, vendingIDS, heightAdjustable, EffectId, IsRare, ClothingId, ExtraRot, rawInteraction));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                Console.ReadKey();
                                Logging.WriteLine("Could not load item #" + Convert.ToInt32(Row[0]) + ", please verify the data is okay.");
                            }
                        }
                    }
                }
            });

            //log.Info("Item Manager -> LOADED");
        }
        private static int SafeToInt(object value, int defaultValue = 0)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;
            string str = value.ToString().Trim();
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            return int.TryParse(str, out int result) ? result : defaultValue;
        }

        private static double SafeToDouble(object value, double defaultValue = 0.0)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;
            string str = value.ToString().Trim().Replace(',', '.');
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            return double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : defaultValue;
        }

        private static bool SafeToBool(object value, bool defaultValue = false)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue;
            string str = value.ToString().Trim();
            if (string.IsNullOrEmpty(str))
                return defaultValue;
            // Asume que "1", "true", "yes" son true; cualquier otra cosa false.
            return str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase) || str.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
        public bool GetItem(string Name, out ItemData Item)
        {
            Item = null;
            if (this._items.Values.Where(x => x.ItemName.ToLower() == Name.ToLower()).ToList().Count > 0)
            {
                Item = this._items.Values.FirstOrDefault(x => x.ItemName.ToLower() == Name.ToLower());
                return true;
            }

            return false;
        }

        public bool GetItem(int id, out ItemData item)
        {
            return _items.TryGetValue(id, out item);
        }

        public ItemData GetItemByName(string name)
        {
            foreach (var entry in this._items)
            {
                ItemData item = entry.Value;
                if (item.ItemName == name)
                    return item;
            }

            return null;
        }

        public bool GetGift(int SpriteId, out ItemData Item)
        {
            if (this._gifts.TryGetValue(SpriteId, out Item))
                return true;
            return false;
        }

        public void DownloadFurnis()
        {
            #region Variables
            XDocument xDoc = XDocument.Load(@"furnidata_updated.xml");
            string ItemName = "";
            string Type = "s";
            int SpriteId = 1;
            int XDim = 0;
            int YDim = 0;
            string PublicName = "";
            string Description = "";
            string AdURL = "";
            string CustomParams = "";
            int SpecialType = 0;
            bool ExcludedDynamic = false;
            bool CanStandOn = false;
            bool CanSitOn = false;
            bool CanLayOn = false;
            string FurniLine = "";
            #endregion

            #region Execute
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                var downloadRoomList = xDoc.Descendants("roomitemtypes").Descendants("furnitype");
                var downloadWallList = xDoc.Descendants("wallitemtypes").Descendants("furnitype");

                if (downloadRoomList.ToList().Count > 0)
                {
                    foreach (var downloadRoomItem in downloadRoomList)
                    {
                        #region Set Variables
                        try
                        {
                            ItemName = downloadRoomItem
                                .Attribute("classname")
                                .Value;
                            SpriteId = Convert.ToInt32(downloadRoomItem
                                .Attribute("id")
                                .Value);
                            PublicName = downloadRoomItem
                                .Element("name")
                                .Value;
                            Description = downloadRoomItem
                                .Element("description")
                                .Value;
                            SpecialType = Convert.ToInt32(downloadRoomItem
                                .Element("specialtype")
                                .Value);
                            AdURL = downloadRoomItem
                                .Element("adurl")
                                .Value;
                            CustomParams = downloadRoomItem
                                .Element("customparams")
                                .Value;
                            XDim = Convert.ToInt32(downloadRoomItem
                                .Element("xdim")
                                .Value);
                            YDim = Convert.ToInt32(downloadRoomItem
                                .Element("ydim")
                                .Value);
                            ExcludedDynamic = PolarEnvironment.EnumToBool(downloadRoomItem
                                .Element("excludeddynamic")
                                .Value);
                            CanLayOn = PolarEnvironment.EnumToBool(downloadRoomItem
                                .Element("canlayon")
                                .Value);
                            CanSitOn = PolarEnvironment.EnumToBool(downloadRoomItem
                                .Element("cansiton")
                                .Value);
                            CanStandOn = PolarEnvironment.EnumToBool(downloadRoomItem
                                .Element("canstandon")
                                .Value);
                            FurniLine = downloadRoomItem
                                .Element("furniline")
                                .Value;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        #endregion

                        #region Insert Query
                        try
                        {
                            dbClient.SetQuery("INSERT INTO `furniture_new`" +
                                "(id,item_name,public_name,type,width,length,can_sit,can_lay,is_walkable,sprite_id,description,specialtype,customparams,excludeddynamic,adurl,furniline) VALUES " +
                                "(@id,@item_name,@public_name,@type,@width,@length,@can_sit,@can_lay,@is_walkable,@sprite_id,@description,@specialtype,@customparams,@excludeddynamic,@adurl,@furniline) ON DUPLICATE KEY UPDATE " +
                                "id = VALUES(id)," +
                                "item_name = VALUES(item_name)," +
                                "public_name = VALUES(public_name)," +
                                "type = VALUES(type)," +
                                "width = VALUES(width)," +
                                "length = VALUES(length)," +
                                "can_sit = VALUES(can_sit)," +
                                "can_lay = VALUES(can_lay)," +
                                "is_walkable = VALUES(is_walkable)," +
                                "sprite_id = VALUES(sprite_id)," +
                                "description = VALUES(description)," +
                                "specialtype = VALUES(specialtype)," +
                                "customparams = VALUES(customparams)," +
                                "excludeddynamic = VALUES(excludeddynamic)," +
                                "adurl = VALUES(adurl)," +
                                "furniline = VALUES(furniline);");
                            dbClient.AddParameter("id", SpriteId);
                            dbClient.AddParameter("item_name", ItemName);
                            dbClient.AddParameter("public_name", PublicName);
                            dbClient.AddParameter("type", Type);
                            dbClient.AddParameter("width", XDim);
                            dbClient.AddParameter("length", YDim);
                            dbClient.AddParameter("can_sit", PolarEnvironment.BoolToEnum(CanSitOn));
                            dbClient.AddParameter("can_lay", PolarEnvironment.BoolToEnum(CanLayOn));
                            dbClient.AddParameter("is_walkable", PolarEnvironment.BoolToEnum(CanStandOn));
                            dbClient.AddParameter("sprite_id", SpriteId);
                            dbClient.AddParameter("description", Description);
                            dbClient.AddParameter("specialtype", SpecialType);
                            dbClient.AddParameter("customparams", CustomParams);
                            dbClient.AddParameter("excludeddynamic", PolarEnvironment.BoolToEnum(ExcludedDynamic));
                            dbClient.AddParameter("adurl", AdURL);
                            dbClient.AddParameter("furniline", FurniLine);
                            dbClient.RunQuery();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        #endregion
                    }
                    //log.Info("Downloaded Room Items");
                    Out.WriteLine("Elementos de la habitación descargados.", "Polar.HabboHotel", ConsoleColor.DarkGray);
                }

                if (downloadWallList.ToList().Count > 0)
                {
                    foreach (var downloadWallItem in downloadWallList)
                    {
                        #region Set Variables
                        try
                        {
                            ItemName = downloadWallItem
                                .Attribute("classname")
                                .Value;
                            SpriteId = Convert.ToInt32(downloadWallItem
                                .Attribute("id")
                                .Value);
                            PublicName = downloadWallItem
                                .Element("name")
                                .Value;
                            Description = downloadWallItem
                                .Element("description")
                                .Value;
                            SpecialType = Convert.ToInt32(downloadWallItem
                                .Element("specialtype")
                                .Value);
                            FurniLine = downloadWallItem
                                .Element("furniline")
                                .Value;
                            Type = "i";
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        #endregion

                        #region Insert Query
                        try
                        {
                            dbClient.SetQuery("INSERT INTO `furniture_new`" +
                                "(id,item_name,public_name,type,width,length,can_sit,can_lay,is_walkable,sprite_id,description,specialtype,customparams,excludeddynamic,adurl,furniline) VALUES " +
                                "(@id,@item_name,@public_name,@type,@width,@length,@can_sit,@can_lay,@is_walkable,@sprite_id,@description,@specialtype,@customparams,@excludeddynamic,@adurl,@furniline) ON DUPLICATE KEY UPDATE " +
                                "id = VALUES(id)," +
                                "item_name = VALUES(item_name)," +
                                "public_name = VALUES(public_name)," +
                                "type = VALUES(type)," +
                                "width = VALUES(width)," +
                                "length = VALUES(length)," +
                                "can_sit = VALUES(can_sit)," +
                                "can_lay = VALUES(can_lay)," +
                                "is_walkable = VALUES(is_walkable)," +
                                "sprite_id = VALUES(sprite_id)," +
                                "description = VALUES(description)," +
                                "specialtype = VALUES(specialtype)," +
                                "customparams = VALUES(customparams)," +
                                "excludeddynamic = VALUES(excludeddynamic)," +
                                "adurl = VALUES(adurl)," +
                                "furniline = VALUES(furniline);");
                            dbClient.AddParameter("id", (100000 + SpriteId));
                            dbClient.AddParameter("item_name", ItemName);
                            dbClient.AddParameter("public_name", PublicName);
                            dbClient.AddParameter("type", Type);
                            dbClient.AddParameter("width", 0);
                            dbClient.AddParameter("length", 0);
                            dbClient.AddParameter("can_sit", 0);
                            dbClient.AddParameter("can_lay", 0);
                            dbClient.AddParameter("is_walkable", 0);
                            dbClient.AddParameter("sprite_id", SpriteId);
                            dbClient.AddParameter("description", Description);
                            dbClient.AddParameter("specialtype", SpecialType);
                            dbClient.AddParameter("customparams", "");
                            dbClient.AddParameter("excludeddynamic", 0);
                            dbClient.AddParameter("adurl", AdURL);
                            dbClient.AddParameter("furniline", FurniLine);
                            dbClient.RunQuery();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        #endregion
                    }
                    //log.Info("Elementos de pared descargados");
                    Out.WriteLine("Elementos de pared descargados.", "Polar.HabboHotel", ConsoleColor.DarkGray);
                }
            }
            #endregion
        }

        public void UpdateFurniSpecial()
        {
            XDocument xmlFile = XDocument.Load(@"http://localhost/furnidata_xml.xml");
            var query = from c in xmlFile.Descendants("roomitemtypes").Descendants("furnitype") select c;
            var query2 = from c in xmlFile.Descendants("wallitemtypes").Descendants("furnitype") select c;

            #region Room Items
            try
            {
                foreach (XElement book in query)
                {
                    string SpriteId = book.Attribute("id").Value;
                    book.Element("offerid").Value = SpriteId;
                    book.Element("bc").Value = "1";
                    book.Element("buyout").Value = "1";
                    book.Element("rentofferid").Value = "-1";
                    book.Element("rentbuyout").Value = "0";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            log.Info("Updated furnidata special for Room Items");
            #endregion

            #region Wall Items
            try
            {
                foreach (XElement book in query2)
                {
                    string SpriteId = book.Attribute("id").Value;
                    book.Element("offerid").Value = (100000 + Convert.ToInt32(SpriteId)).ToString();
                    book.Element("bc").Value = "1";
                    book.Element("buyout").Value = "1";
                    book.Element("rentofferid").Value = "-1";
                    book.Element("rentbuyout").Value = "0";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            log.Info("Updated furnidata special for Wall Items");
            #endregion

            xmlFile.Save("furnidata_updated.xml");
        }

        public void ProductDataMaker()
        {
            #region Variables
            XDocument xmlFile = XDocument.Load(@"furnidata_updated.xml");
            var query = from c in xmlFile.Descendants("roomitemtypes").Descendants("furnitype") select c;
            var query2 = from c in xmlFile.Descendants("wallitemtypes").Descendants("furnitype") select c;

            Dictionary<string, List<ProductData>> Items = new Dictionary<string, List<ProductData>>();
            #endregion

            #region Room Items
            try
            {
                foreach (XElement book in query)
                {
                    string FurniLine = book.Element("furniline").Value.ToLower();
                    string ItemName = book.Attribute("classname").Value;
                    string PublicName = book.Element("name").Value;
                    string Description = book.Element("description").Value;

                    if (!Items.ContainsKey(FurniLine))
                        Items.Add(FurniLine, new List<ProductData>());

                    var Dictionary = Items[FurniLine];

                    ProductData Data = new ProductData(ItemName, PublicName, Description);

                    if (!Dictionary.Contains(Data))
                        Dictionary.Add(Data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            log.Info("Created Productdata Dictionary for Room Items");
            #endregion

            #region Wall Items
            try
            {
                foreach (XElement book in query)
                {
                    string FurniLine = book.Element("furniline").Value.ToLower();
                    string ItemName = book.Attribute("classname").Value;
                    string PublicName = book.Element("name").Value;
                    string Description = book.Element("description").Value;

                    if (!Items.ContainsKey(FurniLine))
                        Items.Add(FurniLine, new List<ProductData>());

                    var Dictionary = Items[FurniLine];

                    ProductData Data = new ProductData(ItemName, PublicName, Description);

                    if (!Dictionary.Contains(Data))
                        Dictionary.Add(Data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            log.Info("Created Productdata Dictionary for Floor Items");
            #endregion

            #region Write Productdata
            if (Items.Count > 0)
            {
                StringBuilder String = new StringBuilder();

                foreach (var Item in Items)
                {
                    String.Append("[");

                    string FurniLine = Item.Key;
                    List<ProductData> Data = Item.Value;

                    if (Data.Count > 0)
                    {
                        string quote = @"""";

                        int Count = 0;
                        foreach (var ProductData in Data)
                        {
                            Count++;
                            String.Append("[" + quote + ProductData.ItemName + quote + "," + quote + ProductData.PublicName + quote + "," + quote + ProductData.Description + quote + "]");

                            if (Count < Data.Count)
                                String.Append(",");
                        }
                    }

                    String.Append("]\n");
                }

                ConsoleWriter.Writer.WriteProductData(String.ToString());
                log.Info("Successfully wrote new productdata!");
            }
            else
                log.Info("Dictionary has no values in it!");
            #endregion
        }
    }

    public class ProductData
    {
        public string ItemName;
        public string PublicName;
        public string Description;

        public ProductData(string ItemName, string PublicName, string Description)
        {
            this.ItemName = ItemName;
            this.PublicName = PublicName;
            this.Description = Description;
        }
    }
}