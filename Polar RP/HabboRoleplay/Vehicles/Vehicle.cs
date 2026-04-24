using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Vehicles
{
    /// <summary>
    /// Structure for weapons
    /// </summary>
    public class Vehicle
    {
        #region Variables
        public uint ID;
        public int ItemID;
        public string ItemName;
        public int EffectID;
        public int Price;
        public string Model;
        public string DisplayName;
        public int MaxFuel;
        public int MaxTrunks;
        public int CarType;
        public int MaxDoors;
        public int CarCorp;
        public int FastCar;

        #endregion

        /// <summary>
        /// Vehicle constructor
        /// </summary>
        public Vehicle(uint ID, int ItemID, string ItemName, int EffectID, int Price, string Model, string DisplayName, int MaxFuel, int MaxTrunks, int CarType, int MaxDoors, int CarCorp, int fastCar)
        {
            this.ID = ID;
            this.ItemID = ItemID;
            this.ItemName = ItemName;
            this.EffectID = EffectID;
            this.Price = Price;
            this.Model = Model;
            this.DisplayName = DisplayName;
            this.MaxFuel = MaxFuel;
            this.MaxTrunks = MaxTrunks;
            this.CarType = CarType;
            this.MaxDoors = MaxDoors;
            this.CarCorp = CarCorp;
            this.FastCar = fastCar;
        }
    }
}
