using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using log4net;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Microsoft.Win32;

namespace Polar.HabboRoleplay.ProductOwned
{
    public class ProductsOwnedManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.PhoneOwned");


        /// <summary>
        /// Thread-safe dictionary containing all houses
        /// </summary>
        public static ConcurrentDictionary<int, ProductsOwned> _ProductsOwned = new ConcurrentDictionary<int, ProductsOwned>();

        /// <summary>
        /// Initializes the house list dictionary
        /// </summary>
        public void Init()
        {
            _ProductsOwned.Clear();

            DataTable PhOwn;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `play_products_owned`");
                PhOwn = DB.getTable();

                if (PhOwn != null)
                {
                    foreach (DataRow Row in PhOwn.Rows)
                    {
                        int Id = Convert.ToInt32(Row["id"]);
                        int ProductId = Convert.ToInt32(Row["product_id"]);
                        int UserId = Convert.ToInt32(Row["user_id"]);
                        string Extrada = Convert.ToString(Row["extradata"]);

                        ProductsOwned newPhOwn = new ProductsOwned(Id, ProductId, UserId, Extrada);
                        _ProductsOwned.TryAdd(Id, newPhOwn);
                    }
                }
            }
            log.Info("Loaded " + _ProductsOwned.Count + " ProductsOwned");
        }

        public List<ProductsOwned> getAllProductsOwned()
        {
            List<ProductsOwned> PO = new List<ProductsOwned>();

            foreach (var item in _ProductsOwned)
            {
                PO.Add(item.Value);
            }
            return PO;
        }

        // Obtener el elemento con el key del diccionario
        public ProductsOwned getProductsOwned(int ID)
        {
            if (_ProductsOwned.ContainsKey(ID))
                return _ProductsOwned[ID];
            else
                return null;
        }

        // Obtener una lista del elemento del diccionario con el key
        public List<ProductsOwned> getProductsOwnedList(int Id)
        {
            List<ProductsOwned> PO = new List<ProductsOwned>();

            lock (_ProductsOwned)
            {
                if (_ProductsOwned.Values.Where(x => x.Id == Id).ToList().Count > 0)
                    PO.Add(_ProductsOwned.Values.FirstOrDefault(x => x.Id == Id));
            }
            return PO;
        }

        // Obtener una lista del elemento del diccionario con el ProductId
        public List<ProductsOwned> getProductsOwnedByProductId(int ProductId)
        {
            List<ProductsOwned> PO = new List<ProductsOwned>();

            foreach (var phone in _ProductsOwned)
            {
                if (phone.Value.ProductId == ProductId)
                    PO.Add(phone.Value);
            }
            return PO;
        }

        // Método para Crear el producto en DB play_products_owned (Al comprar productos nuevos)
        public bool TryCreateProductOwned(GameClient Session, int ProductId, int UserId, string Extradata, out ProductsOwned PO)
        {
            PO = null;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                PO = new ProductsOwned(0, ProductId, UserId, Extradata);

                dbClient.SetQuery("INSERT INTO `play_products_owned` (`product_id`, `user_id`, `extradata`) VALUES (@productid, @owner, @extra)");
                dbClient.AddParameter("productid", ProductId);
                dbClient.AddParameter("owner", UserId);
                dbClient.AddParameter("extra", Extradata);

                PO.Id = Convert.ToInt32(dbClient.InsertQuery());

                if (!_ProductsOwned.TryAdd(PO.Id, PO))
                    return false;
            }
            return true;
        }

        // Devuelve lista con los productos de cierto dueño
        public List<ProductsOwned> getMyProductsOwned(int OwnerId)
        {
            List<ProductsOwned> PO = new List<ProductsOwned>();

            foreach (var item in _ProductsOwned)
            {
                if (item.Value.UserId == OwnerId)
                    PO.Add(item.Value);
            }
            return PO;
        }

        // Devuelve lista con un tipo de producto de cierto dueño
        public List<ProductsOwned> getMyProductsOwnedByProductId(int OwnerId, int ProductId)
        {
            List<ProductsOwned> PO = new List<ProductsOwned>();

            foreach (var item in _ProductsOwned)
            {
                if (item.Value.UserId == OwnerId && item.Value.ProductId == ProductId)
                    PO.Add(item.Value);
            }
            return PO;
        }
    }
}
