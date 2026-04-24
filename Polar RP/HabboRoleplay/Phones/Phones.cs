using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Phones
{
    /// <summary>
    /// Structure for phones
    /// </summary>
    public class Phone
    {
        #region Variables
        public uint ID;
        public string ModelName;
        public string DisplayName;
        public int Price;
        public int EffectID;
        public int ScreenSlots;
        public int DockSlots;
        #endregion

        /// <summary>
        /// Phones constructor
        /// </summary>
        public Phone(uint ID, string ModelName, string DisplayName, int Price, int EffectID, int ScreenSlots, int DockSlots)
        {
            this.ID = ID;
            this.ModelName = ModelName;
            this.DisplayName = DisplayName;
            this.Price = Price;
            this.EffectID = EffectID;
            this.ScreenSlots = ScreenSlots;
            this.DockSlots = DockSlots;
        }
    }
}
