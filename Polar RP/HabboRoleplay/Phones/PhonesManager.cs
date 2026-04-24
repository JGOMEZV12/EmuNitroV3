using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboRoleplay.Phones
{
    public static class PhoneManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Phones");

        /// <summary>
        /// Thread-safe dictionary containing all the phones
        /// </summary>
        public static ConcurrentDictionary<string, Phone> Phones;

        /// <summary>
        /// List containing all phones enables
        /// </summary>
        public static List<int> Enables;

        /// <summary>
        /// Initializes the phone manager
        /// </summary>
        public static void Initialize()
        {
            if (Phones == null)
            {
                Phones = new ConcurrentDictionary<string, Phone>();
                Enables = new List<int>();
            }
            else
            {
                Phones.Clear();
                Enables.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_phones`");
                DataTable PhoneTable = DB.getTable();

                if (PhoneTable == null)
                    log.Error("¡Error al cargar Phones de la DB!");
                else
                    ProcessPhonesTable(PhoneTable);
            }
        }

        /// <summary>
        /// Creates an instance of the phone and stores it in the dictionary
        /// </summary>
        /// <param name="PhoneTable"></param>
        private static void ProcessPhonesTable(DataTable PhoneTable)
        {
            foreach (DataRow Row in PhoneTable.Rows)
            {
                uint ID = Convert.ToUInt32(Row["id"]);
                string ModelName = Convert.ToString(Row["model_name"]);
                string DisplayName = Convert.ToString(Row["display_name"]);
                int Price = Convert.ToInt32(Row["price"]);
                int EffectID = Convert.ToInt32(Row["effect_id"]);
                int ScreenSlots = Convert.ToInt32(Row["screen_slots"]);
                int DockSlots = Convert.ToInt32(Row["dock_slots"]);

                if (Phones.ContainsKey(ModelName))
                    continue;

                Phone Phone = new Phone(ID, ModelName, DisplayName, Price, EffectID, ScreenSlots, DockSlots);
                Phones.TryAdd(ModelName, Phone);

                Enables.Add(Phone.EffectID);
            }

            log.Info("Loaded " + Phones.Count + " roleplay phones.");
        }

        /// <summary>
        /// Gets the phone based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Phone getPhone(string name)
        {
            if (Phones.ContainsKey(name))
                return Phones[name];
            else
                return null;
        }

        public static List<Phone> getAllPhones()
        {
            List<Phone> PO = new List<Phone>();

            foreach (var item in Phones)
            {
                PO.Add(item.Value);
            }
            return PO;
        }

        public static List<Phone> getPhoneById(int ID)
        {
            List<Phone> PO = new List<Phone>();

            foreach (var item in Phones)
            {
                if(item.Value.ID == ID)
                    PO.Add(item.Value);
            }

            return PO;
        }
    }
}
