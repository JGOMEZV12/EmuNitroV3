using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Skins
{
    /// <summary>
    /// Structure for weapons
    /// </summary>
    public class WSkin
    {
        #region Variables
        public uint ID;
        public string Name;
        public string Name2;
        public string PublicName;
        public int Energy;
        public int EffectID;
        public int HandItem;

        public int Range;
        public int MinDamage;
        public int MaxDamage;
        public int ClipSize;

        public int Cost;
        public int Stock;
        #endregion

        /// <summary>
        /// Weapon constructor
        /// </summary>
        public WSkin(uint ID, string Name, string Name2, string PublicName, int EffectID, int HandItem, int Range, int MinDamage, int MaxDamage, int ClipSize, int Cost, int Stock)
        {
            this.ID = ID;
            this.Name = Name;
            this.Name2 = Name2;
            this.PublicName = PublicName;
            this.EffectID = EffectID;
            this.HandItem = HandItem;

            this.Range = Range;
            this.MinDamage = MinDamage;
            this.MaxDamage = MaxDamage;
            this.ClipSize = ClipSize;

            this.Cost = Cost;
            this.Stock = Stock;
        }
    }
}
