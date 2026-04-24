using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboRoleplay.Wizards
{
    public static class HechizosManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Hechizos");

        /// <summary>
        /// Thread-safe dictionary containing all the weapons
        /// </summary>
        public static ConcurrentDictionary<string, Hechizos> Hechizos;

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
            if (Hechizos == null)
            {
                Hechizos = new ConcurrentDictionary<string, Hechizos>();
            }
            else
            {
                Hechizos.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_hechizos`");
                DataTable WeaponTable = DB.getTable();

                if (WeaponTable == null)
                    log.Error("Falló al cargar las armas");
                else
                    ProcessHechizosTable(WeaponTable);
            }
        }

        /// <summary>
        /// Creates an instance of the weapon and stores it in the dictionary
        /// </summary>
        /// <param name="WeaponTable"></param>
        private static void ProcessHechizosTable(DataTable WeaponTable)
        {
            foreach (DataRow Row in WeaponTable.Rows)
            {
                uint ID = Convert.ToUInt32(Row["id"]);

                string WeaponUnfriendlyName = Convert.ToString(Row["name"]);
                string WeaponName = Convert.ToString(Row["publicname"]);
                string Message = Convert.ToString(Row["message"]);
                int Power = Convert.ToInt32(Row["power"]);
                int FiringRange = Convert.ToInt32(Row["firingrange"]);
                int Shields = Convert.ToInt32(Row["shields"]);
                int FiringDamage = Convert.ToInt32(Row["firingdamage"]);
                int Health = Convert.ToInt32(Row["health"]);
               
                int Cost = Convert.ToInt32(Row["cost"]);
                int CostFine = Convert.ToInt32(Row["costfine"]);
                int Stock = Convert.ToInt32(Row["stock"]);

                if (Hechizos.ContainsKey(WeaponUnfriendlyName))
                    continue;

                Hechizos Weapon = new Hechizos(ID, WeaponUnfriendlyName, WeaponName, Message, Power, FiringRange, Shields, FiringDamage, Health, Cost, CostFine, Stock);
                Hechizos.TryAdd(WeaponUnfriendlyName, Weapon);
            }

            log.Info("» Cargado(s): " + Hechizos.Count + " Hechizos.");
        }

        /// <summary>
        /// Gets the weapon based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Hechizos getWizard(string name)
        {
            if (Hechizos.ContainsKey(name))
                return Hechizos[name];
            else
                return null;
        }
    }
}
