using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;
using Polar.HabboRoleplay.Misc;
using Polar.Database.Interfaces;

namespace Polar.HabboRoleplay.Comodin
{
    public static class ComodinManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Comodin");

        /// <summary>
        /// Thread-safe dictionary containing all the comodin
        /// </summary>
        public static ConcurrentDictionary<int, Comodin> Comodines;


        /// <summary>
        /// Initializes the comodin manager
        /// </summary>
        public static void Initialize()
        {
            if (Comodines == null)
            {
                Comodines = new ConcurrentDictionary<int, Comodin>();
            }
            else
            {
                Comodines.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_comodin`");
                DataTable VehicleTable = DB.getTable();

                if (VehicleTable == null)
                    log.Error("¡Error al cargar Comoines de la DB!");
                else
                    ProcessTable(VehicleTable);
            }
        }

        /// <summary>
        /// Creates an instance of the comodin and stores it in the dictionary
        /// </summary>
        /// <param name="VehicleTable"></param>
        private static void ProcessTable(DataTable VehicleTable)
        {
            foreach (DataRow Row in VehicleTable.Rows)
            {
                int ID = Convert.ToInt32(Row["id"]);
                int FurniID = Convert.ToInt32(Row["furni_id"]);
                int RoomID = Convert.ToInt32(Row["room_id"]);
                string Action = Convert.ToString(Row["action"]);

                Comodin Comodin = new Comodin(ID, FurniID, RoomID, Action);
                Comodines.TryAdd(FurniID, Comodin);

            }

            log.Info("Loaded " + Comodines.Count + " roleplay comodines.");
        }

        /// <summary>
        /// Gets the comodin based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Comodin getComodin(int ID)
        {
            if (Comodines.ContainsKey(ID))
                return Comodines[ID];
            else
                return null;
        }
    }
}
