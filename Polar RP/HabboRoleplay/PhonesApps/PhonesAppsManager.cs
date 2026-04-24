using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;
using System.IO;
using System.Text;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.PhonesApps
{
    public static class PhoneAppManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.PhonesApps");

        /// <summary>
        /// Thread-safe dictionary containing all the phones
        /// </summary>
        public static ConcurrentDictionary<string, PhoneApp> PhonesApps;

        /// <summary>
        /// Initializes the phone manager
        /// </summary>
        public static void Initialize()
        {
            if (PhonesApps == null)
            {
                PhonesApps = new ConcurrentDictionary<string, PhoneApp>();
            }
            else
            {
                PhonesApps.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_phones_apps`");
                DataTable PhoneAppTable = DB.getTable();

                if (PhoneAppTable == null)
                    log.Error("¡Error al cargar PhonesApps de la DB!");
                else
                    ProcessPhonesAppsTable(PhoneAppTable);
            }
        }

        /// <summary>
        /// Creates an instance of the phoneapp and stores it in the dictionary
        /// </summary>
        /// <param name="PhoneAppTable"></param>
        private static void ProcessPhonesAppsTable(DataTable PhoneAppTable)
        {
            foreach (DataRow Row in PhoneAppTable.Rows)
            {
                int ID = Convert.ToInt32(Row["id"]);
                string Name = Convert.ToString(Row["name"]);
                string DisplayName = Convert.ToString(Row["display_name"]);
                string Icon = Convert.ToString(Row["icon"]);
                string DeveloperName = Convert.ToString(Row["developer_name"]);
                string Code = Convert.ToString(Row["code"]);
                int Price = Convert.ToInt32(Row["price"]);
                string Version = Convert.ToString(Row["version"]);

                if (PhonesApps.ContainsKey(Name))
                    continue;

                PhoneApp PhoneApp = new PhoneApp(ID, Name, DisplayName, Icon, DeveloperName, Code, Price,Version);
                PhonesApps.TryAdd(Name, PhoneApp);

                #region Generate or update App PHP (OFF) Thanks P3X for the API <3
                /*
                try
                {
                    // Create the file, or overwrite if the file exists.
                    using (FileStream fs = File.Create(RoleplayManager.PhoneAppsPath + Name + ".php"))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(Code);
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                */
                #endregion
            }

            log.Info("Loaded " + PhonesApps.Count + " roleplay phones apps.");
        }

        /// <summary>
        /// Gets the phoneapp based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PhoneApp getPhoneApp(string name)
        {
            if (PhonesApps.ContainsKey(name))
                return PhonesApps[name];
            else
                return null;
        }

        public static List<PhoneApp> getAllPhonesApps()
        {
            List<PhoneApp> APP = new List<PhoneApp>();

            foreach (var item in PhonesApps)
            {
                APP.Add(item.Value);
            }
            return APP;
        }

        public static List<PhoneApp> getPhoneAppById(int AppId)
        {
            List<PhoneApp> APP = new List<PhoneApp>();

            foreach (var item in PhonesApps)
            {
                if(item.Value.ID == AppId)
                    APP.Add(item.Value);
            }
            return APP;
        }
    }
}
