using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;


namespace Polar.HabboHotel.Catalog.Clothing
{
    public class ClothingManager
    {
        private Dictionary<int, ClothingItem> _clothing;

        public ClothingManager()
        {
            this._clothing = new Dictionary<int, ClothingItem>();
           
            this.Init();
        }

        public void Init()
        {
            if (this._clothing.Count > 0)
                this._clothing.Clear();

            DataTable GetClothing = null;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery($"SELECT * FROM `{Polar.Core.DatabaseCompatibility.CatalogClothingTable}`");
                GetClothing = dbClient.getTable();
            }

            if (GetClothing != null)
            {
                foreach (DataRow Row in GetClothing.Rows)
                {
                    int id = Convert.ToInt32(Row["id"]);
                    string name = Row.Table.Columns.Contains("clothing_name") ? Convert.ToString(Row["clothing_name"]) : (Row.Table.Columns.Contains("name") ? Convert.ToString(Row["name"]) : "");
                    string parts = Row.Table.Columns.Contains("clothing_parts") ? Convert.ToString(Row["clothing_parts"]) : (Row.Table.Columns.Contains("parts") ? Convert.ToString(Row["parts"]) : "");
                    int cost = Row.Table.Columns.Contains("cost") ? Convert.ToInt32(Row["cost"]) : 0;

                    this._clothing.Add(id, new ClothingItem(id, name, parts, cost));
                }
            }
        }

        public bool TryGetClothing(int ItemId, out ClothingItem Clothing)
        {
            if (this._clothing.TryGetValue(ItemId, out Clothing))
                return true;
            return false;
        }
    }
}
