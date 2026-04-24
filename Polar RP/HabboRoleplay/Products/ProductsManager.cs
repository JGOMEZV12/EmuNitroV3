using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboRoleplay.Products
{
    public static class ProductsManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Products");

        /// <summary>
        /// Thread-safe dictionary containing all the Products
        /// </summary>
        public static ConcurrentDictionary<string, Product> Products;


        /// <summary>
        /// Initializes the Product manager
        /// </summary>
        public static void Initialize()
        {
            if (Products == null)
            {
                Products = new ConcurrentDictionary<string, Product>();
            }
            else
            {
                Products.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_products`");
                DataTable PhoneTable = DB.getTable();

                if (PhoneTable == null)
                    log.Error("¡Error al cargar Products de la DB!");
                else
                    ProcessPhonesTable(PhoneTable);
            }
        }

        /// <summary>
        /// Creates an instance of the Product and stores it in the dictionary
        /// </summary>
        /// <param name="PhoneTable"></param>
        private static void ProcessPhonesTable(DataTable PhoneTable)
        {
            foreach (DataRow Row in PhoneTable.Rows)
            {
                int ID = Convert.ToInt32(Row["id"]);
                string ProductName = Convert.ToString(Row["name"]);
                string DisplayName = Convert.ToString(Row["display_name"]);
                int Price = Convert.ToInt32(Row["price"]);
                string Type = Convert.ToString(Row["type"]);
                bool CanStack = PolarEnvironment.EnumToBool(Convert.ToString(Row["can_stack"]));
                int MaxCant = Convert.ToInt32(Row["max_cant"]);


                if (Products.ContainsKey(ProductName))
                    continue;

                Product Product = new Product(ID, ProductName, DisplayName, Price, Type, CanStack, MaxCant);
                Products.TryAdd(ProductName, Product);
            }

            log.Info("Loaded " + Products.Count + " roleplay products.");
        }

        /// <summary>
        /// Gets the Product based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Product getProduct(string name)
        {
            if (Products.ContainsKey(name))
                return Products[name];
            else
                return null;
        }

        public static List<Product> getAllProducts()
        {
            List<Product> PO = new List<Product>();

            foreach (var item in Products)
            {
                PO.Add(item.Value);
            }
            return PO;
        }

        public static List<Product> getProductsByType(string Type)
        {
            List<Product> PO = new List<Product>();

            foreach (var item in Products)
            {
                if (item.Value.Type == Type)
                    PO.Add(item.Value);
            }
            return PO;
        }
    }
}
