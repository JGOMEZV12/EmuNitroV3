using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Products
{
    /// <summary>
    /// Structure for Products
    /// </summary>
    public class Product
    {
        #region Variables
        public int ID;
        public string ProductName;
        public string DisplayName;
        public int Price;
        public string Type;
        public bool CanStack;
        public int MaxCant;
        #endregion

        /// <summary>
        /// Products constructor
        /// </summary>
        public Product(int ID, string ProductName, string DisplayName, int Price, string Type, bool CanStack, int MaxCant)
        {
            this.ID = ID;
            this.ProductName = ProductName;
            this.DisplayName = DisplayName;
            this.Price = Price;
            this.Type = Type;
            this.CanStack = CanStack;
            this.MaxCant = MaxCant;
        }
    }
}
