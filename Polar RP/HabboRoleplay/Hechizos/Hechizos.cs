using System;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.Wizards
{
    /// <summary>
    /// Structure for weapons
    /// </summary>
    public class Hechizos
    {
        #region Variables
        public uint ID;
        public string Name;
        public string PublicName;
        public string Message;
        public int Power;
        public int FiringRange;
        public int Shields;
        public int FiringDamage;
        public int Health;

        public int Cost;
        public int CostFine;
        public int Stock;

        #endregion

        /// <summary>
        /// Weapon constructor
        /// </summary>
        public Hechizos(uint ID, string name, string publicName, string message, int power, int firingRange, int shields, int firingDamage, int health, int Cost, int CostFine, int Stock)
        {
            this.ID = ID;
            this.Name = name;
            this.PublicName = publicName;
            this.Message = message;
            this.Power = power;
            this.FiringRange = firingRange;
            this.Shields = shields;
            this.FiringDamage = firingDamage;
            this.Health = health;


            this.Cost = Cost;
            this.CostFine = CostFine;
            this.Stock = Stock;
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

    }
}
