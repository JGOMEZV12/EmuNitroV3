using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using log4net;
using Polar.HabboRoleplay.Misc;
using Polar.Database.Interfaces;

namespace Polar.HabboRoleplay.VehiclesJobs
{
    public static class VehicleJobsManager
    {
        private static readonly ILog log = LogManager.GetLogger("Plus.HabboRoleplay.VehicleJobs");

        /// <summary>
        /// Thread-safe dictionary containing all the weapons
        /// </summary>
        public static ConcurrentDictionary<int, VehicleJobs> Vehicles;


        /// <summary>
        /// Initializes the vehicle manager
        /// </summary>
        public static void Initialize()
        {
            if (Vehicles == null)
            {
                Vehicles = new ConcurrentDictionary<int, VehicleJobs>();
            }
            else
            {
                Vehicles.Clear();
            }

            using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_jobs_cars`");
                DataTable VehicleTable = DB.getTable();

                if (VehicleTable == null)
                    log.Error("¡Error al cargar Vehicles Jobs de la DB!");
                else
                    ProcessVehiclesTable(VehicleTable);
            }
            SpawnAllCars();
        }

        /// <summary>
        /// Creates an instance of the vehicle and stores it in the dictionary
        /// </summary>
        /// <param name="VehicleTable"></param>
        private static void ProcessVehiclesTable(DataTable VehicleTable)
        {
            foreach (DataRow Row in VehicleTable.Rows)
            {
                int ID = Convert.ToInt32(Row["id"]);
                int RoomID = Convert.ToInt32(Row["room_id"]);
                int BaseItem = Convert.ToInt32(Row["base_item"]);
                int X = Convert.ToInt32(Row["x"]);
                int Y = Convert.ToInt32(Row["y"]);
                double Z = Convert.ToDouble(Row["z"]);
                int Rot = Convert.ToInt32(Row["rot"]);
                int JobID = Convert.ToInt32(Row["job_id"]);

                VehicleJobs Vehicle = new VehicleJobs(ID, RoomID, BaseItem, X, Y, Z, Rot, JobID);
                Vehicles.TryAdd(ID, Vehicle);

            }

            log.Info("Loaded " + Vehicles.Count + " roleplay jobs vehicles.");
        }

        private static void SpawnAllCars()
        {
            int lastitem = 0;
            foreach (var item in Vehicles)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    if (item.Value.BaseItem != lastitem)
                    {
                        dbClient.RunQuery("DELETE FROM items WHERE base_item = " + item.Value.BaseItem);
                        lastitem = item.Value.BaseItem;
                        //Console.WriteLine("Eliminamos todos los " + item.Value.BaseItem);
                    }
                    dbClient.SetQuery("INSERT INTO `items` (`user_id`,`room_id`,`base_item`,`x`,`y`,`z`,`rot`) VALUES (@userid,@roomid,@baseitem,@x,@y,@z,@rot)");
                    dbClient.AddParameter("userid", "1");
                    dbClient.AddParameter("roomid", item.Value.RoomID);
                    dbClient.AddParameter("baseitem", item.Value.BaseItem);
                    dbClient.AddParameter("x", item.Value.X);
                    dbClient.AddParameter("y", item.Value.Y);
                    dbClient.AddParameter("z", item.Value.Z);
                    dbClient.AddParameter("rot", item.Value.Rot);
                    dbClient.RunQuery();
                    //Console.WriteLine("Insertamos " + item.Value.ID);
                }
            }
        }

        /// <summary>
        /// Gets the vehicle based on its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static VehicleJobs getVehicleJob(int ID)
        {
            if (Vehicles.ContainsKey(ID))
                return Vehicles[ID];
            else
                return null;
        }

        public static VehicleJobs getVehicleJobBy(int RoomId, int JobId, int X, int Y, double Z)
        {
            foreach (var item in Vehicles)
            {
                if (item.Value.RoomID == RoomId && item.Value.JobID == JobId && item.Value.X == X && item.Value.Y == Y && item.Value.Z == Z)
                    return getVehicleJob(item.Value.ID);
            }
            return null;
        }

        public static int getVehicleJobID(int RoomId, int JobId, int X, int Y)
        {
            foreach (var item in Vehicles)
            {
                if (item.Value.RoomID == RoomId && item.Value.JobID == JobId && item.Value.X == X && item.Value.Y == Y)
                    return item.Value.ID;
            }
            return 0;
        }

        public static int getVehicleJobIDByPos(int RoomId, int X, int Y)
        {
            foreach (var item in Vehicles)
            {
                if (item.Value.RoomID == RoomId && item.Value.X == X && item.Value.Y == Y)
                    return item.Value.ID;
            }
            return 0;
        }
    }
}
