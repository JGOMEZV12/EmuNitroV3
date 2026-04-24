using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.PhonesApps
{
    /// <summary>
    /// Structure for phonesapps
    /// </summary>
    public class PhoneApp
    {
        #region Variables
        public int ID;
        public string Name;
        public string DisplayName;
        public string Icon;
        public string DeveloperName;
        public string Code;
        public int Price;
        public string Version;
        #endregion

        /// <summary>
        /// Phones constructor
        /// </summary>
        public PhoneApp(int ID, string Name, string DisplayName, string Icon, string DeveloperName, string Code, int Price, string Version)
        {
            this.ID = ID;
            this.Name = Name;
            this.DisplayName = DisplayName;
            this.Icon = Icon;
            this.DeveloperName = DeveloperName;
            this.Code = Code;
            this.Price = Price;
            this.Version = Version;
        }
    }
}
