using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboRoleplay.Skins
{
    public static class WSkinManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.WSkin");

        /// <summary>
        /// Thread-safe dictionary containing all the weapons
        /// </summary>
        public static ConcurrentDictionary<string, WSkin> WSkins;

        /// <summary>
        /// List containing all weapon enables
        /// </summary>
        public static List<int> Enables;

        /// <summary>
        /// List containing all weapon handitems
        /// </summary>
        public static List<int> HandItems;

        /// <summary>
        /// Initializes the weapon manager
        /// </summary>
        public static void Initialize()
        {
            if (WSkins == null)
            {
                WSkins = new ConcurrentDictionary<string, WSkin>();
                Enables = new List<int>();
                HandItems = new List<int>();
            }
            else
            {
                WSkins.Clear();
                Enables.Clear();
                HandItems.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_weapons_skins`");
                DataTable WeaponTable = DB.getTable();

                if (WeaponTable == null)
                    log.Error("Falló al cargar las skins de las armas");
                else
                    ProcessWSkinsTable(WeaponTable);
            }
        }

        /// <summary>
        /// Creates an instance of the weapon and stores it in the dictionary
        /// </summary>
        /// <param name="WeaponTable"></param>
        private static void ProcessWSkinsTable(DataTable WeaponSkinTable)
        {
            foreach (DataRow Row in WeaponSkinTable.Rows)
            {
                uint ID = Convert.ToUInt32(Row["id"]);

                string WeaponUnfriendlyName = Convert.ToString(Row["name"]);
                string WeaponUnfriendlyName2 = Convert.ToString(Row["name2"]);
                string WeaponName = Convert.ToString(Row["publicname"]);
                int EffectID = Convert.ToInt32(Row["effectid"]);
                int HandItem = Convert.ToInt32(Row["handitem"]);

                int Range = Convert.ToInt32(Row["firingrange"]);
                int MinDamage = Convert.ToInt32(Row["mindamage"]);
                int MaxDamage = Convert.ToInt32(Row["maxdamage"]);
                int ClipSize = Convert.ToInt32(Row["clipsize"]);

                int Cost = Convert.ToInt32(Row["cost"]);
                int Stock = Convert.ToInt32(Row["stock"]);

                if (WSkins.ContainsKey(WeaponUnfriendlyName))
                    continue;

                WSkin Weapon = new WSkin(ID, WeaponUnfriendlyName, WeaponUnfriendlyName2, WeaponName, EffectID, HandItem, Range, MinDamage, MaxDamage, ClipSize, Cost, Stock);
                WSkins.TryAdd(WeaponUnfriendlyName, Weapon);

                Enables.Add(Weapon.EffectID);
                HandItems.Add(Weapon.HandItem);
            }

            log.Info("» Cargado(s): " + WSkins.Count + " skin de armas.");
        }

        /// <summary>
        /// Gets the weapon based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static WSkin getWeapon(string name)
        {
            if (WSkins.ContainsKey(name))
                return WSkins[name];
            else
                return null;
        }
    }
}
