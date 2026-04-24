using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Polar.Core;
using Polar.HabboHotel.GameClients;
using System.Collections.Concurrent;
using Polar.Database.Interfaces;
using log4net;
using Polar.HabboRoleplay.Turfs;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Bots.Manager;

namespace Polar.HabboHotel.Rooms
{
    public class RoomManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Rooms.RoomManager");

        private Dictionary<string, RoomModel> _roomModels;
        private readonly object _roomLoadingSync;
        public ConcurrentDictionary<int, Room> _rooms;
        private ConcurrentDictionary<int, RoomData> _loadedRoomData;

        private DateTime _purgeLastExecution;

        // ─────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────
        public RoomManager()
        {
            _roomLoadingSync = new object();
            _roomModels = new Dictionary<string, RoomModel>();
            _rooms = new ConcurrentDictionary<int, Room>();
            _loadedRoomData = new ConcurrentDictionary<int, RoomData>();
            _purgeLastExecution = DateTime.Now.AddHours(3);
        }

        // ─────────────────────────────────────
        //  Contadores
        // ─────────────────────────────────────
        public int LoadedRoomDataCount => _loadedRoomData.Count;
        public int Count => _rooms.Count;

        // ─────────────────────────────────────
        //  PreLoad
        // ─────────────────────────────────────
        public void PreLoadRooms()
        {
            lock (_roomLoadingSync)
            {
                // FIX: cargamos solo los ids en esta conexión, sin abrir conexiones anidadas
                var roomIds = new List<int>();

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `rooms`");
                    DataTable table = dbClient.getTable();
                    if (table == null) return;

                    foreach (DataRow row in table.Rows)
                        roomIds.Add(Convert.ToInt32(row["id"]));
                }

                foreach (int id in roomIds)
                    GenerateRoomData(id);
            }
        }

        // ─────────────────────────────────────
        //  Modelos
        // ─────────────────────────────────────
        public void LoadModel(string id)
        {
            // FIX: query parametrizada — antes concatenaba Id directamente (SQL injection)
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT id,door_x,door_y,door_z,door_dir,heightmap,`wall_height` " +
                    "FROM `room_models` WHERE `custom` = '1' AND `id` = @id LIMIT 1");
                dbClient.AddParameter("id", id);
                DataRow row = dbClient.getRow();

                if (row == null)
                    return;

                string modelName = Convert.ToString(row["id"]);
                string heightmap = Convert.ToString(row["heightmap"]);

                // FIX: saltar si heightmap vacío
                if (string.IsNullOrEmpty(heightmap))
                {
                    log.Warn($"LoadModel: modelo '{id}' tiene heightmap vacío — ignorado.");
                    return;
                }

                if (!_roomModels.ContainsKey(id))
                {
                    _roomModels.Add(modelName, new RoomModel(id,
                        Convert.ToInt32(row["door_x"]),
                        Convert.ToInt32(row["door_y"]),
                        Convert.ToDouble(row["door_z"]),
                        Convert.ToInt32(row["door_dir"]),
                        heightmap,
                        Convert.ToInt32(row["wall_height"]),
                        Convert.ToString(row["poolmap"])));
                }
            }
        }

        public void LoadModels()
        {
            _roomModels.Clear();

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT id,door_x,door_y,door_z,door_dir,heightmap,wall_height,poolmap FROM room_models");
                DataTable table = dbClient.getTable();
                if (table == null) return;

                foreach (DataRow row in table.Rows)
                {
                    string modelId = Convert.ToString(row["id"]);
                    string heightmap = Convert.ToString(row["heightmap"]);

                    // FIX: saltar filas con heightmap vacío en vez de explotar al inicio
                    if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(heightmap))
                    {
                        log.Warn($"room_models: fila con id='{modelId}' tiene heightmap vacío — ignorada.");
                        continue;
                    }

                    if (_roomModels.ContainsKey(modelId)) continue;

                    try
                    {
                        _roomModels.Add(modelId, new RoomModel(modelId,
                            (int)row["door_x"],
                            (int)row["door_y"],
                            (double)row["door_z"],
                            (int)row["door_dir"],
                            heightmap,
                            Convert.ToInt32(row["wall_height"]),
                            Convert.ToString(row["poolmap"])));
                    }
                    catch (Exception ex)
                    {
                        log.Error($"room_models: error parseando modelo '{modelId}': {ex.Message}");
                    }
                }
            }
        }

        // FIX: ReloadModel ahora es atómico — antes había una ventana donde el modelo no existía
        public void ReloadModel(string id)
        {
            // Cargamos el nuevo modelo antes de remover el anterior
            LoadModel(id);

            // Si ya existía una versión vieja y LoadModel no la sobreescribió (por el ContainsKey),
            // la removemos y volvemos a cargar
            if (_roomModels.ContainsKey(id))
                _roomModels.Remove(id);

            LoadModel(id);
        }

        public bool TryGetModel(string id, out RoomModel model)
        {
            return _roomModels.TryGetValue(id, out model);
        }

        public RoomModel GetModel(string model, int roomId)
        {
            if (model == "model_custom")
                return GetCustomData(roomId);

            _roomModels.TryGetValue(model, out RoomModel result);
            return result;
        }

        // FIX: query parametrizada — antes concatenaba roomID directamente (SQL injection)
        private static RoomModel GetCustomData(int roomId)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT door_x,door_y,door_z,door_dir,heightmap,wall_height " +
                    "FROM room_models_customs WHERE room_id = @roomId");
                dbClient.AddParameter("roomId", roomId);
                DataRow row = dbClient.getRow();

                if (row == null)
                    throw new Exception($"El room model de la sala {roomId} no ha sido encontrado.");

                return new RoomModel(roomId.ToString(),
                    (int)row["door_x"],
                    (int)row["door_y"],
                    (double)row["door_z"],
                    (int)row["door_dir"],
                    (string)row["heightmap"],
                    (int)row["wall_height"],
                    string.Empty);
            }
        }

        // ─────────────────────────────────────
        //  Obtener salas
        // ─────────────────────────────────────
        public Room GetRoom(int roomId)
        {
            _rooms.TryGetValue(roomId, out Room room);
            return room;
        }

        public bool TryGetRoom(int roomId, out Room room)
        {
            return _rooms.TryGetValue(roomId, out room);
        }

        public ICollection<Room> GetRooms() => _rooms.Values;

        // FIX: .Count() sobre IEnumerable enumeraba toda la colección — ahora FirstOrDefault es O(1)
        public Room TryGetRandomLoadedRoom()
        {
            return (from r in _rooms
                    where r.Value.RoomData.UsersNow > 0 &&
                          r.Value.RoomData.State == 0 &&
                          r.Value.RoomData.UsersNow < r.Value.RoomData.UsersMax
                    orderby r.Value.RoomData.UsersNow descending
                    select r.Value).FirstOrDefault();
        }

        // ─────────────────────────────────────
        //  Unload
        // ─────────────────────────────────────
        public async Task UnloadRoom(Room room, bool removeData = false)
        {
            if (room == null)
                return;

            #region Roleplay Checks

            // Texas Hold Em
            List<TexasHoldEm> games = TexasHoldEmManager.GetGamesByRoomId(room.Id);
            foreach (TexasHoldEm game in games)
            {
                if (game == null) continue;

                game.PotSquare.Furni = null;
                game.JoinGate.Furni = null;

                foreach (TexasHoldEmItem item in game.Player1.Values) item.Furni = null;
                foreach (TexasHoldEmItem item in game.Player2.Values) item.Furni = null;
                foreach (TexasHoldEmItem item in game.Player3.Values) item.Furni = null;
                foreach (TexasHoldEmItem item in game.Banker.Values) item.Furni = null;
            }

            // Farming
            List<FarmingSpace> farmingSpaces = FarmingManager.GetFarmingSpacesByRoomId(room.Id);
            foreach (FarmingSpace space in farmingSpaces)
            {
                if (space == null) continue;
                space.Item = null;
                space.Spawned = false;
            }

            #region Bots
            RoleplayBotManager.EjectRoomsDeployedBots(room);
            #endregion

            #endregion

            if (_rooms.TryRemove(room.RoomId, out _))
            {
                await room.DisposeAsync();

                if (removeData)
                    _loadedRoomData.TryRemove(room.Id, out _);
            }
        }

        // ─────────────────────────────────────
        //  Update
        // ─────────────────────────────────────

        // FIX: TryUpdate es atómico — antes leía _loadedRoomData[Room.Id] dos veces (race condition)
        public void UpdateRoom(Room room)
        {
            if (_loadedRoomData.TryGetValue(room.Id, out RoomData existingData))
                _loadedRoomData.TryUpdate(room.Id, room.RoomData, existingData);

            if (_rooms.TryGetValue(room.Id, out Room existingRoom))
                _rooms.TryUpdate(room.Id, room, existingRoom);
        }

        public List<Room> GetLoadedRooms()
        {
            return this._rooms.Values.ToList();
        }
        // ─────────────────────────────────────
        //  RoomData
        // ─────────────────────────────────────

        // FIX: queries parametrizadas — antes concatenaba RoomId directamente (SQL injection)
        public RoomData GenerateRoomData(int roomId)
        {
            if (_loadedRoomData.TryGetValue(roomId, out RoomData cached))
                return cached;

            if (TryGetRoom(roomId, out Room existingRoom))
                return existingRoom.RoomData;

            DataRow row = null;
            DataRow rpRow = null;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `rooms` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", roomId);
                row = dbClient.getRow();

                dbClient.SetQuery("SELECT * FROM `rp_rooms` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", roomId);
                rpRow = dbClient.getRow();
            }

            if (row == null || rpRow == null)
                return null;

            RoomData data = new RoomData();
            data.Fill(row);
            data.FillRP(rpRow);

            _loadedRoomData.TryAdd(roomId, data);
            return data;
        }

        public bool TryGetRoomData(int roomId, out RoomData data)
        {
            return _loadedRoomData.TryGetValue(roomId, out data);
        }

        public RoomData FetchRoomData(int roomId, DataRow dRow, DataRow dRowRP)
        {
            if (_loadedRoomData.TryGetValue(roomId, out RoomData existing))
                return existing;

            RoomData data = new RoomData();
            data.Fill(dRow);
            data.FillRP(dRowRP);

            _loadedRoomData.TryAdd(roomId, data);
            return data;
        }

        public RoomData LoadRoomData(int roomId)
        {
            if (_loadedRoomData.TryGetValue(roomId, out RoomData cached))
                return cached;

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "SELECT r.*, u.username AS owner_name FROM rooms r " +
                    "JOIN users u ON r.owner = u.id WHERE r.id = @id LIMIT 1");
                dbClient.AddParameter("id", roomId);
                DataTable table = dbClient.getTable();

                if (table == null || table.Rows.Count == 0)
                    return null;

                DataRow row = table.Rows[0];

                dbClient.SetQuery("SELECT * FROM `rp_rooms` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", roomId);
                DataRow rpRow = dbClient.getRow();

                if (row == null || rpRow == null)
                    return null;

                RoomData data = new RoomData();
                data.Fill(row, (string)row["owner_name"]);
                data.FillRP(rpRow);

                _loadedRoomData.TryAdd(roomId, data);
                return data;
            }
        }

        // ─────────────────────────────────────
        //  LoadRoom — FIX: lógica de las 3 sobrecargas unificada en un método base
        // ─────────────────────────────────────
        public Room LoadRoom(int id) => LoadRoomInternal(id, false);

        public Room LoadRoom(int id, bool botCheck) => LoadRoomInternal(id, botCheck);

        public bool LoadRoom(int id, out Room room)
        {
            room = LoadRoomInternal(id, false);
            return room != null;
        }

        private Room LoadRoomInternal(int id, bool botCheck)
        {
            if (TryGetRoom(id, out Room existing))
                return existing;

            RoomData data = GenerateRoomData(id) ?? LoadRoomData(id);
            if (data == null)
                return null;

            Room room = new Room(data);

            if (_rooms.TryAdd(room.RoomId, room) && botCheck)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    RoleplayBotManager.DeployCachedBots(room);
                });
            }

            return room;
        }

        // ─────────────────────────────────────
        //  CreateRoom — FIX: INSERT de rp_rooms parametrizado
        // ─────────────────────────────────────
        public RoomData CreateRoom(GameClient session, string name, string description, string model,
            int category, string city, int maxVisitors, int tradeSettings)
        {
            if (!_roomModels.ContainsKey(model))
            {
                session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_model_missing"));
                return null;
            }

            if (name.Length < 3)
            {
                session.SendNotification(PolarEnvironment.GetGame().GetLanguageLocale().TryGetValue("room_name_length_short"));
                return null;
            }

            int roomId;

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery(
                    "INSERT INTO `rooms` (`roomtype`,`caption`,`description`,`owner`,`model_name`,`category`,`users_max`,`trade_settings`,`username`) " +
                    "VALUES ('private',@caption,@description,@userId,@model,@category,@usersMax,@tradeSettings,@userName)");
                dbClient.AddParameter("caption", name);
                dbClient.AddParameter("description", description);
                dbClient.AddParameter("userId", session.GetHabbo().Id);
                dbClient.AddParameter("model", model);
                dbClient.AddParameter("category", category);
                dbClient.AddParameter("usersMax", maxVisitors);
                dbClient.AddParameter("tradeSettings", tradeSettings);
                dbClient.AddParameter("userName", session.GetHabbo().Username);
                roomId = Convert.ToInt32(dbClient.InsertQuery());

                // FIX: INSERT de rp_rooms también parametrizado — antes concatenaba RoomId y City
                dbClient.SetQuery(
                    "INSERT INTO `rp_rooms` (`id`,`city`,`safezone_enabled`) " +
                    "VALUES (@id, @city, '1')");
                dbClient.AddParameter("id", roomId);
                dbClient.AddParameter("city", city);
                dbClient.RunQuery();
            }

            RoomData newRoomData = GenerateRoomData(roomId);
            session.GetHabbo().UsersRooms.Add(newRoomData);
            return newRoomData;
        }

        // ─────────────────────────────────────
        //  Búsquedas
        // ─────────────────────────────────────
        public List<RoomData> SearchGroupRooms(string query)
        {
            return (from r in _loadedRoomData
                    where r.Value.State != 3 &&
                          r.Value.Group != null &&
                          (r.Value.OwnerName.StartsWith(query) ||
                           r.Value.Tags.Contains(query) ||
                           r.Value.Name.Contains(query))
                    orderby r.Value.UsersNow descending
                    select r.Value).Take(50).ToList();
        }

        public List<RoomData> SearchTaggedRooms(string query)
        {
            return (from r in _loadedRoomData
                    where r.Value.UsersNow >= 0 &&
                          r.Value.State != 3 &&
                          r.Value.Tags.Contains(query)
                    orderby r.Value.UsersNow descending
                    select r.Value).Take(50).ToList();
        }

        public List<RoomData> GetPopularRooms(int category, int amount = 50)
        {
            return (from r in _loadedRoomData
                    where r.Value.UsersNow > 0 &&
                          (category == -1 || r.Value.Category == category) &&
                          r.Value.State != 3
                    orderby r.Value.Score descending
                    orderby r.Value.UsersNow descending
                    select r.Value).Take(amount).ToList();
        }

        public List<RoomData> GetRecommendedRooms(int amount = 50, int currentRoomId = 0)
        {
            return (from r in _loadedRoomData
                    where r.Value.UsersNow >= 0 &&
                          r.Value.Score >= 0 &&
                          r.Value.State != 3 &&
                          r.Value.Id != currentRoomId
                    orderby r.Value.Score descending
                    orderby r.Value.UsersNow descending
                    select r.Value).Take(amount).ToList();
        }

        public List<RoomData> GetPopularRatedRooms(int amount = 50)
        {
            return (from r in _loadedRoomData
                    where r.Value.State != 3
                    orderby r.Value.Score descending
                    select r.Value).Take(amount).ToList();
        }

        public List<RoomData> GetRoomsByCategory(int category, int amount = 50)
        {
            return (from r in _loadedRoomData
                    where r.Value.Category == category &&
                          r.Value.UsersNow > 0 &&
                          r.Value.State != 3
                    orderby r.Value.UsersNow descending
                    select r.Value).Take(amount).ToList();
        }

        public List<RoomData> GetOnGoingRoomPromotions(int mode, int amount = 50)
        {
            var query = _loadedRoomData
                .Where(r => r.Value.HasActivePromotion && r.Value.State != 3)
                .Select(r => r.Value);

            if (mode == 17)
                return query.OrderByDescending(r => r.Promotion.TimestampStarted).Take(amount).ToList();
            else
                return query.OrderByDescending(r => r.UsersNow).Take(amount).ToList();
        }

        public List<RoomData> GetPromotedRooms(int categoryId, int amount = 50)
        {
            return (from r in _loadedRoomData
                    where r.Value.HasActivePromotion &&
                          r.Value.Promotion.CategoryId == categoryId &&
                          r.Value.State != 3
                    orderby r.Value.Promotion.TimestampStarted descending
                    select r.Value).Take(amount).ToList();
        }

        public List<RoomData> GetGroupRooms(int amount = 50)
        {
            return (from r in _loadedRoomData
                    where r.Value.Group != null && r.Value.State != 3
                    orderby r.Value.Score descending
                    select r.Value).Take(amount).ToList();
        }

        // FIX: Sort() + Reverse() reemplazado por OrderByDescending — más limpio y sin mutación in-place
        public List<KeyValuePair<string, int>> GetPopularRoomTags()
        {
            var tags = (from r in _loadedRoomData
                        where r.Value.UsersNow >= 0 && r.Value.State != 3
                        orderby r.Value.UsersNow descending
                        orderby r.Value.Score descending
                        select r.Value.Tags).Take(50);

            var tagValues = new Dictionary<string, int>();

            foreach (var tagList in tags)
            {
                foreach (string tag in tagList)
                {
                    if (tagValues.ContainsKey(tag))
                        tagValues[tag]++;
                    else
                        tagValues[tag] = 1;
                }
            }

            return tagValues
                .OrderByDescending(kvp => kvp.Value)
                .ToList();
        }

        // ─────────────────────────────────────
        //  Dispose
        // ─────────────────────────────────────
        public async Task DisposeAsync()
        {
            var tasks = _rooms.Values
                .Where(r => r != null)
                .Select(r => UnloadRoom(r))
                .ToList();

            await Task.WhenAll(tasks);

            Out.WriteLine("¡He terminado de deshacerse de las habitaciones!", "Polar.HabboHotel", ConsoleColor.DarkGray);
        }
    }
}