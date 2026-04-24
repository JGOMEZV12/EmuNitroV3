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
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboRoleplay.VehicleOwned
{
    public class VehiclesOwnedManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private readonly ILog log = LogManager.GetLogger("Polar.HabboRoleplay.PhoneChat.VehiclesOwnedManager");
        

        /// <summary>
        /// Thread-safe dictionary containing all houses
        /// </summary>
        public static ConcurrentDictionary<int, VehiclesOwned> _VehiclesOwned = new ConcurrentDictionary<int, VehiclesOwned>();

        /// <summary>
        /// Initializes the house list dictionary
        /// </summary>
        public void Init()
        {
            _VehiclesOwned.Clear();
            
            DataTable VhOwn;

            using (IQueryAdapter DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * FROM `rp_vehicles_owned`");
                VhOwn = DB.getTable();

                if (VhOwn != null)
                {
                    foreach (DataRow Row in VhOwn.Rows)
                    {
                        int Id = Convert.ToInt32(Row["id"]);
                        int FurniId = Convert.ToInt32(Row["furni_id"]);
                        int ItemId = Convert.ToInt32(Row["item_id"]);
                        int OwnerId = Convert.ToInt32(Row["owner"]);
                        int LastUserId = Convert.ToInt32(Row["last_user"]);
                        string Model = Convert.ToString(Row["model"]);
                        int Fuel = Convert.ToInt32(Row["fuel"]);                        
                        int Km = Convert.ToInt32(Row["km"]);                        
                        int State = Convert.ToInt32(Row["state"]);
                        bool Traba = PolarEnvironment.EnumToBool(Row["traba"].ToString());
                        bool Alarm = PolarEnvironment.EnumToBool(Row["alarm"].ToString());
                        int Location = Convert.ToInt32(Row["location"]);
                        int X = Convert.ToInt32(Row["x"]);
                        int Y = Convert.ToInt32(Row["y"]);
                        double Z = Convert.ToDouble(Row["z"]);
                        string[] Baul = Row["baul"].ToString().Split(';');
                        bool BaulOpen = PolarEnvironment.EnumToBool(Row["baul_state"].ToString());
                        int CarLife = Convert.ToInt32(Row["life"]);

                        VehiclesOwned newVhOwn = new VehiclesOwned(Id, FurniId, ItemId, OwnerId, LastUserId, Model, Fuel, Km, State, Traba, Alarm, Location, X, Y, Z, Baul, BaulOpen, CarLife);
                        _VehiclesOwned.TryAdd(Id, newVhOwn);
                    }
                }
            }
            log.Info("Loaded "+ _VehiclesOwned.Count + " VehiclesOwned");
        }
        public List<VehiclesOwned> getAllVehiclesOwned()
        {
            List<VehiclesOwned> VO = new List<VehiclesOwned>();

            foreach (var item in _VehiclesOwned)
            {
                    VO.Add(item.Value);
            }
            return VO;
        }
        // Obtener el elemento con el key del diccionario
        public VehiclesOwned getVehiclesOwned(int ID)
        {
            if (_VehiclesOwned.ContainsKey(ID))
                return _VehiclesOwned[ID];
            else
                return null;
        }

        // Obtener una lista del elemento del diccionario con el key
        public List<VehiclesOwned> getVehiclesOwnedList(int Id)
        {
            List<VehiclesOwned> VO = new List<VehiclesOwned>();

            lock (_VehiclesOwned)
            {
                if (_VehiclesOwned.Values.Where(x => x.Id == Id).ToList().Count > 0)
                    VO.Add(_VehiclesOwned.Values.FirstOrDefault(x => x.Id == Id));
            }
            return VO;
        }

        // Obtener una lista del elemento del diccionario con el FurniId
        public List<VehiclesOwned> getVehiclesOwnedByFurniId(int FurniId)
        {
            List<VehiclesOwned> VO = new List<VehiclesOwned>();

            foreach (var item in _VehiclesOwned)
            {
                if (item.Value.FurniId == FurniId)
                    VO.Add(item.Value);
            }
            return VO;
        }

        // Obtener una lista del elemento del diccionario con el CamOwnId
        public List<VehiclesOwned> getVehiclesOwnedByCamOwnId(int CamOwnId)
        {
            List<VehiclesOwned> VO = new List<VehiclesOwned>();

            foreach (var item in _VehiclesOwned)
            {
                if (item.Value.CamOwnId == CamOwnId)
                    VO.Add(item.Value);
            }
            return VO;
        }

        // Agregar nuevo vehículo sólo al Diccionario. No DB. (Para autos CORP)
        public bool NewVehicleOwned(int Id, int FurniId, int ItemId, int OwnerId, int LastUserId, string Model, int Fuel, int Km, int State, bool Traba, bool Alarm, int Location, int X, int Y, double Z, string[] Baul, bool BaulOpen, out VehiclesOwned VO)
        {
            VehiclesOwned newVhOwn = new VehiclesOwned(Id, FurniId, ItemId, OwnerId, LastUserId, Model, Fuel, Km, State, Traba, Alarm, Location, X, Y, Z, Baul, BaulOpen, 100);
            _VehiclesOwned.TryAdd(Id, newVhOwn);
            VO = getVehiclesOwned(Id);
            return (VO != null) ? true : false;
        }
        
        // Método para Crear auto en DB rp_vehicles_owned (Al comprar autos nuevos)
        public bool TryCreateVehicleOwned(GameClient Session, int FurniId, int ItemId, int OwnerId, int LastUserId, string Model, int Fuel, int Km, int State, bool Traba, bool Alarm, int Location, int X, int Y, double Z, string[] Baul, bool BaulOpen, out VehiclesOwned VO)
        {
            VO = null;
            int ItDemId = 0;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO items (user_id,base_item,room_id) VALUES ('"+Session.GetHabbo().Id+"', '" + ItemId + "', '" + Session.GetRoomUser().RoomId + "')");
                dbClient.RunQuery();
                dbClient.SetQuery("SELECT id FROM items WHERE user_id = '" + Session.GetHabbo().Id + "' AND room_id = '" + Session.GetRoomUser().RoomId + "' AND base_item = '" + ItemId + "' ORDER BY id DESC LIMIT 1");
                ItDemId = dbClient.getInteger();
                dbClient.RunQuery("UPDATE items SET user_id = '0' WHERE user_id = '" + Session.GetHabbo().Id + "' AND room_id = '" + Session.GetRoomUser().RoomId + "' AND base_item = '" + ItemId + "'");

                if (ItDemId <= 0)
                    return false;

                VO = new VehiclesOwned(0, ItDemId, ItemId, OwnerId, LastUserId, Model, Fuel, Km, State, Traba, Alarm, Location, X, Y, Z, Baul, BaulOpen, 100);

                dbClient.SetQuery("INSERT INTO `rp_vehicles_owned` (`furni_id`, `item_id`, `owner`, `last_user`, `model`, `fuel`) VALUES (@furniid, @itemid, @owner, @lastuser, @model, @fuel)");
                //dbClient.AddParameter("id", VO.Id);
                dbClient.AddParameter("furniid", VO.FurniId);
                dbClient.AddParameter("itemid", VO.ItemId);
                dbClient.AddParameter("owner", VO.OwnerId);
                dbClient.AddParameter("lastuser", VO.LastUserId);
                dbClient.AddParameter("model", VO.Model);
                dbClient.AddParameter("fuel", VO.Fuel);

                VO.Id = Convert.ToInt32(dbClient.InsertQuery());

                if (Session != null)
                    Session.GetRoleplay().DrivingCarId = VO.Id;

                if (!_VehiclesOwned.TryAdd(VO.Id, VO))
                    return false;
            }
            return true;
        }
        
        // Método para actualizar datos del diccionario para el auto a estacionar.
        public bool UpdateVehicleOwner(GameClient Session, int ItemId, bool ToDB, out VehiclesOwned VOD)
        {
            VOD = null;

            if (Session == null)
                return false;

            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetRoleplay().DrivingCarId);
            if (VO != null && VO.Count > 0)
            {
                //VO[0].FurniId = ItemId;
                VO[0].LastUserId = Session.GetHabbo().Id;
                VO[0].Fuel = Session.GetRoleplay().CarFuel;
                VO[0].Km = Session.GetRoleplay().CarTimer;
                VO[0].Location = (Session.GetRoomUser() != null) ? Session.GetRoomUser().RoomId : 23; // ID de sala de grúa
                VO[0].X = (Session.GetRoomUser() != null) ? Session.GetRoomUser().X : 0;
                VO[0].Y = (Session.GetRoomUser() != null) ? Session.GetRoomUser().Y : 0;
                VO[0].Z = (Session.GetRoomUser() != null) ? Session.GetRoomUser().Z : 0;
                VO[0].CarLife = Session.GetRoleplay().CarLife;

                if (ToDB)
                {
                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        //DB.SetQuery("UPDATE `rp_vehicles_owned` SET `furni_id` = @furniid, `last_user` = @lastuser, `fuel` = @fuel, `km` = @km, `location` = @location, `x` = @x, `y` = @y, `z` = @z, `life` = @carlife WHERE `rp_vehicles_owned`.`id` = @id");
                        DB.SetQuery("UPDATE `rp_vehicles_owned` SET `last_user` = @lastuser, `fuel` = @fuel, `km` = @km, `location` = @location, `x` = @x, `y` = @y, `z` = @z, `life` = @carlife WHERE `rp_vehicles_owned`.`id` = @id");
                        DB.AddParameter("id", Session.GetRoleplay().DrivingCarId);
                        //DB.AddParameter("furniid", ItemId);
                        DB.AddParameter("lastuser", Session.GetHabbo().Id);
                        DB.AddParameter("fuel", Session.GetRoleplay().CarFuel);
                        DB.AddParameter("km", Session.GetRoleplay().CarTimer);
                        DB.AddParameter("location", Session.GetRoomUser().RoomId);
                        DB.AddParameter("x", Session.GetRoomUser().X);
                        DB.AddParameter("y", Session.GetRoomUser().Y);
                        DB.AddParameter("z", Session.GetRoomUser().Z);
                        DB.AddParameter("carlife", Session.GetRoleplay().CarLife);
                        DB.RunQuery();
                    }
                }
                VOD = getVehiclesOwned(VO[0].Id);
                return (VOD != null) ? true : false;
            }
            return false;
        }

        // Al desconectarse
        public bool UpdateVehicleOwnerDisc(GameClient Session, int ItemId, bool ToDB, int RoomId, int X, int Y, double Z, out VehiclesOwned VOD)
        {
            VOD = null;

            if (Session == null)
                return false;

            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetRoleplay().DrivingCarId);
            if (VO != null && VO.Count > 0)
            {
                //VO[0].FurniId = ItemId;
                VO[0].LastUserId = Session.GetHabbo().Id;
                VO[0].Fuel = Session.GetRoleplay().CarFuel;
                VO[0].Km = Session.GetRoleplay().CarTimer;
                VO[0].Location = RoomId;
                VO[0].X = X;
                VO[0].Y = Y;
                VO[0].Z = Z;
                VO[0].CarLife = Session.GetRoleplay().CarLife;

                if (ToDB)
                {
                    using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                       // DB.SetQuery("UPDATE `rp_vehicles_owned` SET `furni_id` = @furniid, `last_user` = @lastuser, `fuel` = @fuel, `km` = @km, `location` = @location, `x` = @x, `y` = @y, `z` = @z, `life` = @carlife WHERE `rp_vehicles_owned`.`id` = @id");
                        DB.SetQuery("UPDATE `rp_vehicles_owned` SET `last_user` = @lastuser, `fuel` = @fuel, `km` = @km, `location` = @location, `x` = @x, `y` = @y, `z` = @z, `life` = @carlife WHERE `rp_vehicles_owned`.`id` = @id");
                        DB.AddParameter("id", Session.GetRoleplay().DrivingCarId);
                        //DB.AddParameter("furniid", ItemId);
                        DB.AddParameter("lastuser", Session.GetHabbo().Id);
                        DB.AddParameter("fuel", Session.GetRoleplay().CarFuel);
                        DB.AddParameter("km", Session.GetRoleplay().CarTimer);
                        DB.AddParameter("location", RoomId);
                        DB.AddParameter("x", X);
                        DB.AddParameter("y", Y);
                        DB.AddParameter("z", Z);
                        DB.AddParameter("carlife", Session.GetRoleplay().CarLife);
                        DB.RunQuery();
                    }
                }
                VOD = getVehiclesOwned(VO[0].Id);
                return (VOD != null) ? true : false;
            }
            return false;
        }

        public bool UpdateVehicleGruaBug(int userid, out VehiclesOwned VOD)
        {
            VOD = null;
            
            List<VehiclesOwned> VO = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(userid);
            if (VO != null && VO.Count > 0)
            {
                foreach (VehiclesOwned _vo in VO)
                {
                    if (_vo.Location == 0)
                    {
                        List<VehiclesOwned> VO2 = PolarEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(_vo.Id);
                        if (VO2 != null && VO2.Count > 0)
                        {
                            VO2[0].Location = 23;// ID de sala de grúa
                            RoleplayManager.UpdateVehicleStat(VO2[0].Id, "location", 23);// ID de sala de grúa

                            VOD = getVehiclesOwned(VO2[0].Id);
                        }
                    }
                }
                return (VOD != null) ? true : false;
            }
            return false;
        }

        // Devuelve lista con los vehículos de cierto dueño
        public List<VehiclesOwned> getMyVehiclesOwned(int OwnerId)
        {
            List<VehiclesOwned> VO = new List<VehiclesOwned>();

            foreach (var item in _VehiclesOwned)
            {
                if (item.Value.OwnerId == OwnerId)
                    VO.Add(item.Value);
            }
            return VO;
        }

        // Elimina registro de diccionario
        public void DeleteVehicleOwned(int Id, bool ToDB = false)
        {
            VehiclesOwned VO = null;
            if (_VehiclesOwned.ContainsKey(Id))
                _VehiclesOwned.TryRemove(Id, out VO);

            if (ToDB)
            {
                using (var DB = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    DB.RunQuery("DELETE FROM `rp_vehicles_owned` WHERE `rp_vehicles_owned`.`id` = '"+ Id +"' LIMIT 1");                    
                }
            }
        }

        public void DeleteVehicleOwnedByFurniId(int FurniId)
        {
            VehiclesOwned VO = null;
            foreach (var item in _VehiclesOwned)
            {
                if (item.Value.FurniId == FurniId)
                    _VehiclesOwned.TryRemove(item.Value.Id, out VO);
            }
            
        }
    }
}
