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

namespace Polar.HabboRoleplay.Houses
{
    public class HouseManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.Houses.HouseManager");

        /// <summary>
        /// Static int used to generate item ids
        /// </summary>
        public int SignMultiplier = 1500000;

        /// <summary>
        /// Thread-safe dictionary containing all houses
        /// </summary>
        public ConcurrentDictionary<int, House> HouseList = new ConcurrentDictionary<int, House>();

        /// <summary>
        /// Initializes the house list dictionary
        /// </summary>
        public void Init()
        {
            HouseList.Clear();
            DataTable Houses;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `rp_houses`");
                Houses = DB.getTable();

                if (Houses != null)
                {
                    foreach (DataRow Row in Houses.Rows)
                    {
                        int ItemId = Convert.ToInt32(Row["sign_id"]);
                        int RoomId = Convert.ToInt32(Row["room_id"]);
                        int OwnerId = Convert.ToInt32(Row["owner_id"]);
                        int Cost = Convert.ToInt32(Row["cost"]);
                        bool ForSale = PolarEnvironment.EnumToBool(Row["for_sale"].ToString());
                        int Level = Convert.ToInt32(Row["level"]);
                        string[] Upgrades = Row["upgrades"].ToString().Split(',');
                        bool IsLocked = PolarEnvironment.EnumToBool(Row["is_locked"].ToString());
                        int InsideRoomId = Convert.ToInt32(Row["inside_room_id"]);
                        int DoorX = Convert.ToInt32(Row["door_x"]);
                        int DoorY = Convert.ToInt32(Row["door_y"]);
                        double DoorZ = Convert.ToDouble(Row["door_z"]);
                        int Type = Convert.ToInt32(Row["type"]);
                        long Last_Forcing = Convert.ToInt32(Row["last_forcing"]);
                        string[] Space = Row["space"].ToString().Split(';');

                        House newHouse = new House(ItemId, RoomId, OwnerId, Cost, ForSale, Level, Upgrades, IsLocked, InsideRoomId, DoorX, DoorY, DoorZ, Type, Last_Forcing, Space);
                        HouseList.TryAdd(ItemId, newHouse);
                    }
                }
            }

            log.Info("Loaded " + HouseList.Count + " houses.");
        }

        #region Posibles mejoras (OFF)
        /*
        public House getHouse(int ID)
        {
            if (HouseList.ContainsKey(ID))
                return HouseList[ID];
            else
                return null;
        }
        public List<House> getHouseList(int Id)
        {
            List<House> House = new List<House>();

            lock (HouseList)
            {
                if (HouseList.Values.Where(x => x.ItemId == Id).ToList().Count > 0)
                    House.Add(HouseList.Values.FirstOrDefault(x => x.ItemId == Id));
            }
            return House;
        }
        public List<House> getHouseByPosition(int RoomId, int X, int Y, double Z)
        {
            List<House> House = new List<House>();

            lock (HouseList)
            {
                if (HouseList.Values.Where(x => x.RoomId == RoomId && x.DoorX == X && x.DoorY == Y && x.DoorZ == Z).ToList().Count > 0)
                    House.Add(HouseList.Values.FirstOrDefault(x => x.RoomId == RoomId && x.DoorX == X && x.DoorY == Y && x.DoorZ == Z));
            }
            return House;
        }
        */
        #endregion

        public List<House> GetHouseByOwnerId(int OwnerId)
        {
            if (OwnerId == 0)
                return null;

            if (HouseList.Values.Where(x => x.OwnerId == OwnerId).ToList().Count > 0)
                return HouseList.Values.Where(x => x.OwnerId == OwnerId).ToList();
            else
                return null;

        }

        public House GetHouseBySignItem(int ItemId)
        {
            if (ItemId == 0)
                return null;

            if (HouseList.Values.Where(x => x.ItemId == ItemId).ToList().Count > 0)
                return HouseList.Values.FirstOrDefault(x => x.ItemId == ItemId);
            else
                return null;
        }

        public List<House> GetHousesBySignRoomId(int roomid)
        {

            if (HouseList.Values.Where(x => x.RoomId == roomid).ToList().Count > 0)
                return HouseList.Values.Where(x => x.RoomId == roomid).ToList();
            else
                return new List<House>();

        }

        public List<House> GetTerrainsBySignRoomId(int roomid)
        {

            if (HouseList.Values.Where(x => x.RoomId == roomid && x.Type == 3).ToList().Count > 0)
                return HouseList.Values.Where(x => x.RoomId == roomid && x.Type == 3).ToList();
            else
                return new List<House>();

        }

        public House GetHouseByPosition(int RoomId, int X, int Y, double Z)
        {

            if (HouseList.Values.Where(x => x.RoomId == RoomId && x.DoorX == X && x.DoorY == Y && x.DoorZ == Z).ToList().Count > 0)
                return HouseList.Values.FirstOrDefault(x => x.RoomId == RoomId && x.DoorX == X && x.DoorY == Y && x.DoorZ == Z);
            else
                return null;
        }

        public House GetHouseByInsideRoom(int insideroom)
        {

            if (HouseList.Values.Where(x => x.InsideRoomId == insideroom).ToList().Count > 0)
                return HouseList.Values.FirstOrDefault(x => x.InsideRoomId == insideroom);
            else
                return null;
        }

        public House GetTerrainByInsideRoom(int insideroom)
        {

            if (HouseList.Values.Where(x => x.InsideRoomId == insideroom && x.Type == 3).ToList().Count > 0)
                return HouseList.Values.FirstOrDefault(x => x.InsideRoomId == insideroom && x.Type == 3);
            else
                return null;
        }
    }
}
