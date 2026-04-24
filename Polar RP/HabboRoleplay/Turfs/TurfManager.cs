using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using log4net;

namespace Polar.HabboRoleplay.Turfs
{
    public class TurfManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Turfs.TurfManager");

        /// <summary>
        /// Thread-safe dictionary containing all roleplay turfs
        /// </summary>
        public static ConcurrentDictionary<int, Turf> TurfList = new ConcurrentDictionary<int, Turf>();

        /// <summary>
        /// Initializes the turf list dictionary
        /// </summary>
        public void Initialize()
        {
            TurfList.Clear();

            DataTable TurfsTable;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_rooms`, `rooms` WHERE rp_rooms.id = rooms.id AND rp_rooms.turf_enabled = '1'");
                TurfsTable = DB.getTable();

                if (TurfsTable != null)
                {
                    foreach (DataRow Turfs in TurfsTable.Rows)
                    {
                        int RoomId = Convert.ToInt32(Turfs["id"]);
                        int GangId = Convert.ToInt32(Turfs["group_id"]);

                        Turf newTurf = new Turf(RoomId, GangId);
                        TurfList.TryAdd(RoomId, newTurf);
                    }
                }
            }

            //log.Info("Loaded " + TurfList.Count + " roleplay turfs.");
            Out.WriteLine("Cargado(s): " + TurfList.Count + " territorios.", "Polar.HabboRoleplay", ConsoleColor.DarkGray);
        }

        /// <summary>
        /// Gets the food based on roomid
        /// </summary>
        /// <param name="roomid"></param>
        /// <returns></returns>
        public static Turf GetTurf(int roomid)
        {
            try
            {
                Turf theturf = null;

                foreach (Turf turf in TurfList.Values)
                {
                    if (turf.RoomId == roomid)
                    {
                        return turf;
                    }
                }

                return theturf;
            }
            catch
            {
                return null;
            }
        }

        public Turf GetTurfById(int roomid)
        {
            try
            {
                Turf theturf = null;

                foreach (Turf turf in TurfList.Values)
                {
                    if (turf.RoomId == roomid)
                    {
                        return turf;
                    }
                }

                return theturf;
            }
            catch
            {
                return null;
            }
        }

        // Obtener el elemento con el key del diccionario
        public Turf getTurfbyRoom(int RoomId)
        {
            if (TurfList.ContainsKey(RoomId))
                return TurfList[RoomId];
            else
                return null;
        }

        public List<Turf> getTurfbyRoomList(int Id)
        {
            List<Turf> VO = new List<Turf>();

            lock (TurfList)
            {
                if (TurfList.Values.Where(x => x.RoomId == Id).ToList().Count > 0)
                    VO.Add(TurfList.Values.FirstOrDefault(x => x.RoomId == Id));
            }
            return VO;
        }

        public List<Turf> getTurfsbyGang(int GangId)
        {
            List<Turf> VO = new List<Turf>();

            foreach (var item in TurfList)
            {
                if (item.Value.GangId == GangId)
                    VO.Add(item.Value);
            }
            return VO;
        }

        // Obtener barrios de X Banda
        public List<Turf> getTurfs()
        {
            List<Turf> VO = new List<Turf>();

            foreach (var item in TurfList)
            {
                    VO.Add(item.Value);
            }
            return VO;
        }

    }
}
