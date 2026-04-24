using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Weapons
{
    /// <summary>
    /// Structure for weapons
    /// </summary>
    public class Weapon
    {
        #region Variables
        public uint ID;
        public string Name;
        public string PublicName;
        public string FiringText;
        public string EquipText;
        public string UnEquipText;
        public string ReloadText;
        public int Energy;
        public int EffectID;
        public int HandItem;

        public int Range;
        public int MinDamage;
        public int MaxDamage;
        public int ClipSize;
        public int ReloadTime;

        public int Cost;
        public int CostFine;
        public int Stock;
        public int LevelRequirement;

        public bool CanUse;
        public int TotalBullets;
        public int WLife;
        public bool isVip;
        public int BaulCar;
        public WeaponCategory Category;

        #endregion

        /// <summary>
        /// Weapon constructor
        /// </summary>
        public Weapon(uint ID, string Name, string PublicName, string FiringText, string EquipText, string UnEquipText, string ReloadText, int Energy, int EffectID, int HandItem, int Range, int MinDamage, int MaxDamage, int ClipSize, int ReloadTime, int Cost, int CostFine, int Stock, int LevelRequirement, bool CanUse, int totalBullets, int wLife, bool Vip, int BaulCar, WeaponCategory category)
        {
            this.ID = ID;
            this.Name = Name;
            this.PublicName = PublicName;
            this.FiringText = FiringText;
            this.EquipText = EquipText;
            this.UnEquipText = UnEquipText;
            this.ReloadText = ReloadText;
            this.Energy = Energy;
            this.EffectID = EffectID;
            this.HandItem = HandItem;

            this.Range = Range;
            this.MinDamage = MinDamage;
            this.MaxDamage = MaxDamage;
            this.ClipSize = ClipSize;
            this.ReloadTime = ReloadTime;

            this.Cost = Cost;
            this.CostFine = CostFine;
            this.Stock = Stock;
            this.LevelRequirement = LevelRequirement;

            this.CanUse = CanUse;
            this.TotalBullets = totalBullets;
            this.WLife = wLife;
            this.isVip = Vip;
            this.BaulCar = BaulCar;
            this.Category = category;
        }

        /// <summary>
        /// Reloads the weapon
        /// </summary>
        /*public bool Reload(GameClient Client, GameClient TargetClient = null)
        {
            Client.GetRoleplay().GunShots = 0;

            if (Client.GetRoleplay().Bullets > 0)
            {
                if (TargetClient != null)
                    RoleplayManager.Shout(Client, "*Intenta dispararle a " + TargetClient.GetHabbo().Username + " pero falla al ver que su cartucho no tiene balas*", 5);

                this.ReloadMessage(Client, this.ClipSize);

                // New Desgaste por recarga
                Client.GetRoleplay().WLife--;
                RoleplayManager.UpdateMyWeaponStats(Client, "life", Client.GetRoleplay().WLife, Client.GetRoleplay().EquippedWeapon.Name);
                if (Client.GetRoleplay().WLife <= 0)
                    RoleplayManager.Shout(Client, "* Mientras " + Client.GetHabbo().Username + " recargaba su arma, se escucha como cruje y se daña.", 4);

                this.WLife = Client.GetRoleplay().WLife;
                return true;
            }
            else
            {
                if (TargetClient != null)
                    RoleplayManager.Shout(Client, "*Intenta dispararle a " + TargetClient.GetHabbo().Username + " pero falla al ver que su arma se quedó sin balas*", 5);

                // New Balas infinitas
                Client.SendWhisper("¡No tienes balas para poder recargar tu arma!", 1);
                //this.ReloadMessage(Client, this.ClipSize);
                RoleplayManager.UpdateMyWeaponStats(Client, "bullets", 0, Client.GetRoleplay().EquippedWeapon.Name);

                // New Desgaste por recarga
                Client.GetRoleplay().WLife--;
                RoleplayManager.UpdateMyWeaponStats(Client, "life", Client.GetRoleplay().WLife, Client.GetRoleplay().EquippedWeapon.Name);
                if (Client.GetRoleplay().WLife <= 0)
                    RoleplayManager.Shout(Client, "* Mientras " + Client.GetHabbo().Username + " recargaba su arma, se escucha como cruje y se daña.", 4);

                this.WLife = Client.GetRoleplay().WLife;
                return false;
            }
        }
        */

        public bool Reload(GameClient Client, GameClient? TargetClient = null)
        {
            Client.GetRoleplay().GunShots = 0;

            if (Client.GetRoleplay().Bullets > 0)
            {
                /*if (TargetClient != null)
                    RoleplayManager.Shout(Client, "*Intenta disparar a " + TargetClient.GetHabbo().Username + " pero falla*", 4);
                */
                this.ReloadMessage(Client, this.ClipSize);
                // New Desgaste por recarga
                Client.GetRoleplay().WLife--;
                RoleplayManager.UpdateMyWeaponStats(Client, "life", Client.GetRoleplay().WLife, Client.GetRoleplay().EquippedWeapon.Name);
                if (Client.GetRoleplay().WLife <= 0)
                    RoleplayManager.Shout(Client, "* Mientras " + Client.GetHabbo().Username + " recargaba su arma, se escucha como cruje y se daña.", 4);

                this.WLife = Client.GetRoleplay().WLife;
                return true;
            }
            else
            {
                if (TargetClient != null)
                    RoleplayManager.Shout(Client, "*Intenta disparar a " + TargetClient.GetHabbo().Username + " Pero se da cuenta de que se le acabaron las balas por completo*", 4);
                else
                    Client.SendWhisper("¡Te has quedado sin balas para recargar tu arma!", 1);
                return false;
            }
        }
        /// <summary>
        /// Provides the reload text depending on bool
        /// </summary>
        /// <param name="Client"></param>
        public void ReloadMessage(GameClient Client, int Bullets)
        {
            string Text = this.ReloadText;
            Text = Text.Replace("[NAME]", PublicName);
            Text = Text.Replace("[BULLETS]", Bullets.ToString());

            RoleplayManager.Shout(Client, Text, 4);
        }
    }
}
