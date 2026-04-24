using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;

namespace Polar.HabboRoleplay.Vehicles
{
    public static class VehicleManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Vehicles");

        /// <summary>
        /// Thread-safe dictionary containing all the weapons
        /// </summary>
        public static ConcurrentDictionary<string, Vehicle> Vehicles;

        /// <summary>
        /// List containing all vehicle enables
        /// </summary>
        public static List<int> Enables;

        /// <summary>
        /// Initializes the vehicle manager
        /// </summary>
        public static void Initialize()
        {
            if (Vehicles == null)
            {
                Vehicles = new ConcurrentDictionary<string, Vehicle>();
                Enables = new List<int>();
            }
            else
            {
                Vehicles.Clear();
                Enables.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_vehicles`");
                DataTable VehicleTable = DB.getTable();

                if (VehicleTable == null)
                    log.Error("¡Error al cargar Vehicles de la DB!");
                else
                    ProcessVehiclesTable(VehicleTable);
            }
        }

        /// <summary>
        /// Creates an instance of the vehicle and stores it in the dictionary
        /// </summary>
        /// <param name="VehicleTable"></param>
        private static void ProcessVehiclesTable(DataTable VehicleTable)
        {
            foreach (DataRow Row in VehicleTable.Rows)
            {
                uint ID = Convert.ToUInt32(Row["id"]);

                int ItemID = Convert.ToInt32(Row["item_id"]);
                string ItemName = Convert.ToString(Row["item_name"]);
                int EffectID = Convert.ToInt32(Row["effect_id"]);
                int Price = Convert.ToInt32(Row["price"]);
                string VehicleUnfriendlyName = Convert.ToString(Row["model"]);
                string DisplayName = Convert.ToString(Row["display_name"]);
                int MaxFuel = Convert.ToInt32(Row["max_fuel"]);
                int MaxTrunks = Convert.ToInt32(Row["max_trunks"]);
                int CarType = Convert.ToInt32(Row["type"]);
                int MaxDoors = Convert.ToInt32(Row["max_passengers"]);
                int CarCorp = Convert.ToInt32(Row["jobid"]);
                int FastCar = Convert.ToInt32(Row["fast"]);


                if (Vehicles.ContainsKey(VehicleUnfriendlyName))
                    continue;

                Vehicle Vehicle = new Vehicle(ID, ItemID, ItemName, EffectID, Price, VehicleUnfriendlyName, DisplayName, MaxFuel, MaxTrunks, CarType, MaxDoors, CarCorp, FastCar);
                Vehicles.TryAdd(VehicleUnfriendlyName, Vehicle);

                Enables.Add(Vehicle.EffectID);
            }

            log.Info("Loaded " + Vehicles.Count + " roleplay vehicles.");
        }

        /// <summary>
        /// Gets the vehicle based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Vehicle getVehicle(string name)
        {
            if (Vehicles.ContainsKey(name))
                return Vehicles[name];
            else
                return null;
        }

        public static List<Vehicle> getAllVehicles()
        {
            List<Vehicle> VH = new List<Vehicle>();

            foreach (var item in Vehicles)
            {
                VH.Add(item.Value);
            }
            return VH;
        }
    }
}
