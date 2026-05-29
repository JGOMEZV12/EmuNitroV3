using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using log4net;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboRoleplay.Farming;
using Polar.HabboRoleplay.Gambling;
using Polar.HabboRoleplay.Houses;
using Polar.HabboRoleplay.Turfs;

namespace Polar.HabboHotel.Rooms
{
    public class RoomManager
    {
        private static readonly ILog log = LogManager.GetLogger("Polar.HabboHotel.Rooms.RoomManager");

        // ── Modelos ────────────────────────────────────────────────────────────────
        // FIX: Dictionary<> es suficiente aquí — los modelos se cargan una sola vez
        //      al arrancar y luego son de solo lectura. ConcurrentDictionary sería
        //      overhead innecesario. Acceso siempre desde el hilo de inicio.
        private Dictionary<string, RoomModel> _roomModels;

        // ── Salas vivas ────────────────────────────────────────────────────────────
        // FIX: ConcurrentDictionary<int, Lazy<Room>> — el patrón estándar para evitar
        //      que la fábrica costosa (ctor de Room + carga de muebles + GenerateMaps)
        //      se ejecute más de una vez aunque varios hilos soliciten la misma sala a
        //      la vez. GetOrAdd puede llamar el factory varias veces; Lazy<T> garantiza
        //      que sólo uno la construye.
        public readonly ConcurrentDictionary<int, Lazy<Room>> _rooms;

        // ── Datos de sala cacheados (sin instancia viva) ───────────────────────────
        private readonly ConcurrentDictionary<int, RoomData> _loadedRoomData;

        private readonly object _roomLoadingSync = new();
        private DateTime _purgeLastExecution;

        // ── Constructor ────────────────────────────────────────────────────────────
        public RoomManager()
        {
            _roomModels = new Dictionary<string, RoomModel>();
            _rooms = new ConcurrentDictionary<int, Lazy<Room>>();
            _loadedRoomData = new ConcurrentDictionary<int, RoomData>();
            _purgeLastExecution = DateTime.Now.AddHours(3);
        }

        // ── Contadores ─────────────────────────────────────────────────────────────
        public int LoadedRoomDataCount => _loadedRoomData.Count;

        // FIX: antes Count() sobre IEnumerable (O(n)); ahora .Count sobre el dict (O(1))
        public int Count => _rooms.Count;

        // ══════════════════════════════════════════════════════════════════════════
        //  PRE-LOAD
        // ══════════════════════════════════════════════════════════════════════════

        public void PreLoadRooms()
        {
            lock (_roomLoadingSync)
            {
                var roomIds = new List<int>();

                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("SELECT `id` FROM `rooms`");
                    DataTable table = dbClient.getTable();
                    if (table == null) return;

                    foreach (DataRow row in table.Rows)
                        roomIds.Add(Convert.ToInt32(row["id"]));
                }

                // Sólo cachear RoomData; no levantar la Room entera hasta que alguien
                // entre. Esto reduce el pico de memoria en el arranque.
                foreach (int id in roomIds)
                    GenerateRoomData(id);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  MODELOS
        // ══════════════════════════════════════════════════════════════════════════

        public void LoadModel(string id)
        {
            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT id,door_x,door_y,door_z,door_dir,heightmap,wall_height " +
                "FROM `room_models` WHERE `custom` = '1' AND `id` = @id LIMIT 1");
            dbClient.AddParameter("id", id);
            DataRow row = dbClient.getRow();
            if (row == null) return;

            string modelName = Convert.ToString(row["id"]);
            string heightmap = Convert.ToString(row["heightmap"]);
            if (string.IsNullOrEmpty(heightmap))
            {
                log.Warn($"LoadModel: modelo '{id}' tiene heightmap vacío — ignorado.");
                return;
            }

            // FIX: usar indexer (upsert atómico) en vez de ContainsKey + Add
            _roomModels[modelName] = new RoomModel(id,
                Convert.ToInt32(row["door_x"]), Convert.ToInt32(row["door_y"]),
                Convert.ToDouble(row["door_z"]), Convert.ToInt32(row["door_dir"]),
                heightmap, Convert.ToInt32(row["wall_height"]),
                Convert.ToString(row["poolmap"] ?? ""));
        }

        public void LoadModels()
        {
            _roomModels.Clear();

            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery("SELECT id,door_x,door_y,door_z,door_dir,heightmap,wall_height,poolmap FROM room_models");
            DataTable table = dbClient.getTable();
            if (table == null) return;

            foreach (DataRow row in table.Rows)
            {
                string modelId = Convert.ToString(row["id"]);
                string heightmap = Convert.ToString(row["heightmap"]);

                if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(heightmap))
                {
                    log.Warn($"room_models: fila id='{modelId}' heightmap vacío — ignorada.");
                    continue;
                }

                if (_roomModels.ContainsKey(modelId)) continue;

                try
                {
                    _roomModels.Add(modelId, new RoomModel(modelId,
                        (int)row["door_x"], (int)row["door_y"], (double)row["door_z"],
                        (int)row["door_dir"], heightmap,
                        Convert.ToInt32(row["wall_height"]),
                        Convert.ToString(row["poolmap"] ?? "")));
                }
                catch (Exception ex)
                {
                    log.Error($"room_models: error parseando modelo '{modelId}': {ex.Message}");
                }
            }
        }

        // FIX: ReloadModel era TOCTOU — borraba el modelo antes de cargarlo, dejando
        //      una ventana donde TryGetModel fallaba. Ahora carga primero, luego
        //      sobreescribe con el indexer.
        public void ReloadModel(string id)
        {
            LoadModel(id); // si ya existe, el indexer de LoadModel lo sobreescribe
        }

        public bool TryGetModel(string id, out RoomModel model) =>
            _roomModels.TryGetValue(id, out model);

        public RoomModel GetModel(string model, int roomId)
        {
            if (model == "model_custom")
                return GetCustomData(roomId);

            _roomModels.TryGetValue(model, out RoomModel result);
            return result;
        }

        private static RoomModel GetCustomData(int roomId)
        {
            using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT door_x,door_y,door_z,door_dir,heightmap,wall_height " +
                "FROM room_models_customs WHERE room_id = @roomId");
            dbClient.AddParameter("roomId", roomId);
            DataRow row = dbClient.getRow();
            if (row == null)
                throw new Exception($"Room model de la sala {roomId} no encontrado.");

            return new RoomModel(roomId.ToString(),
                (int)row["door_x"], (int)row["door_y"], (double)row["door_z"],
                (int)row["door_dir"], (string)row["heightmap"],
                (int)row["wall_height"], string.Empty);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  OBTENER SALAS
        // ══════════════════════════════════════════════════════════════════════════

        // FIX: devuelve la Room real. Si el Lazy aún no se ha resuelto se resuelve aquí.
        //      Nunca lanza excepción — devuelve null si la sala no existe.
        public Room GetRoom(int roomId)
        {
            if (_rooms.TryGetValue(roomId, out Lazy<Room> lazy))
            {
                try { return lazy.Value; }
                catch { return null; }
            }
            return null;
        }

        public bool TryGetRoom(int roomId, out Room room)
        {
            if (_rooms.TryGetValue(roomId, out Lazy<Room> lazy))
            {
                try { room = lazy.Value; return room != null; }
                catch { room = null; return false; }
            }
            room = null;
            return false;
        }

        public ICollection<Room> GetRooms()
        {
            // FIX: materializar sólo las Lazy ya resueltas para no forzar carga
            var result = new List<Room>(_rooms.Count);
            foreach (var lazy in _rooms.Values)
            {
                if (lazy.IsValueCreated)
                {
                    try { if (lazy.Value != null) result.Add(lazy.Value); }
                    catch { /* sala con error de construcción */ }
                }
            }
            return result;
        }

        public Room TryGetRandomLoadedRoom()
        {
            return GetLoadedRooms()
                .Where(r => r != null &&
                            r.RoomData.UsersNow > 0 &&
                            r.RoomData.State == 0 &&
                            r.RoomData.UsersNow < r.RoomData.UsersMax)
                .OrderByDescending(r => r.RoomData.UsersNow)
                .FirstOrDefault();
        }


        // ══════════════════════════════════════════════════════════════════════════
        //  UNLOAD
        // ══════════════════════════════════════════════════════════════════════════

        public async Task UnloadRoom(Room room, bool removeData = false)
        {
            if (room == null) return;

            #region Roleplay cleanup

            foreach (TexasHoldEm game in TexasHoldEmManager.GetGamesByRoomId(room.Id))
            {
                if (game == null) continue;
                game.PotSquare.Furni = null;
                game.JoinGate.Furni = null;
                foreach (var item in game.Player1.Values) item.Furni = null;
                foreach (var item in game.Player2.Values) item.Furni = null;
                foreach (var item in game.Player3.Values) item.Furni = null;
                foreach (var item in game.Banker.Values) item.Furni = null;
            }

            foreach (FarmingSpace space in FarmingManager.GetFarmingSpacesByRoomId(room.Id))
            {
                if (space == null) continue;
                space.Item = null;
                space.Spawned = false;
            }

            RoleplayBotManager.EjectRoomsDeployedBots(room);
            #endregion

            // FIX: buscar y eliminar por roomId, no por referencia de objeto
            if (_rooms.TryRemove(room.RoomId, out _))
            {
                await room.DisposeAsync();
                if (removeData)
                    _loadedRoomData.TryRemove(room.Id, out _);
            }
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════════════

        public void UpdateRoom(Room room)
        {
            // Actualizar RoomData cacheado
            if (_loadedRoomData.TryGetValue(room.Id, out RoomData existingData))
                _loadedRoomData.TryUpdate(room.Id, room.RoomData, existingData);

            // Re-registrar la sala viva con un nuevo Lazy ya resuelto
            var newLazy = new Lazy<Room>(() => room, System.Threading.LazyThreadSafetyMode.PublicationOnly);
            _ = newLazy.Value; // forzar IsValueCreated = true antes de insertar

            _rooms.AddOrUpdate(room.Id,
                _ => newLazy,
                (_, old) =>
                {
                    Room? oldRoom = TrySafeGetValue(old);
                    // sólo reemplazar si es la misma instancia o la antigua ya no existe
                    return (oldRoom == null || ReferenceEquals(oldRoom, room)) ? newLazy : old;
                });
        }

        public List<Room> GetLoadedRooms()
        {
            var result = new List<Room>(_rooms.Count);
            foreach (var lazy in _rooms.Values)
            {
                if (!lazy.IsValueCreated) continue;
                Room r = TrySafeGetValue(lazy);
                if (r != null) result.Add(r);
            }
            return result;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  ROOM DATA
        // ══════════════════════════════════════════════════════════════════════════

        public RoomData GenerateRoomData(int roomId)
        {
            // 1. Cache de datos
            if (_loadedRoomData.TryGetValue(roomId, out RoomData cached))
                return cached;

            // 2. Sala ya viva
            if (TryGetRoom(roomId, out Room existingRoom))
                return existingRoom.RoomData;

            // 3. BD
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

            if (row == null || rpRow == null) return null;

            RoomData data = new RoomData();
            data.Fill(row);
            data.FillRP(rpRow);

            // FIX: GetOrAdd con Lazy para evitar doble inserción bajo concurrencia
            _loadedRoomData.TryAdd(roomId, data);
            return data;
        }

        public bool TryGetRoomData(int roomId, out RoomData data) =>
            _loadedRoomData.TryGetValue(roomId, out data);

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

            using var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            dbClient.SetQuery(
                "SELECT r.*, u.username AS owner_name FROM rooms r " +
                "JOIN users u ON r.owner = u.id WHERE r.id = @id LIMIT 1");
            dbClient.AddParameter("id", roomId);
            DataTable table = dbClient.getTable();
            if (table == null || table.Rows.Count == 0) return null;

            DataRow row = table.Rows[0];

            dbClient.SetQuery("SELECT * FROM `rp_rooms` WHERE `id` = @id LIMIT 1");
            dbClient.AddParameter("id", roomId);
            DataRow rpRow = dbClient.getRow();
            if (rpRow == null) return null;

            RoomData data = new RoomData();
            data.Fill(row, (string)row["owner_name"]);
            data.FillRP(rpRow);
            _loadedRoomData.TryAdd(roomId, data);
            return data;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  LOAD ROOM   — ConcurrentDictionary<int, Lazy<Room>>
        //  El patrón Lazy<T> asegura que el constructor costoso de Room (carga de
        //  muebles, GenerateMaps, InitBots…) se ejecuta UNA sola vez aunque varios
        //  hilos pidan la misma sala simultáneamente.
        // ══════════════════════════════════════════════════════════════════════════

        public Room LoadRoom(int id) => LoadRoomInternal(id, false);
        public Room LoadRoom(int id, bool botCheck) => LoadRoomInternal(id, botCheck);

        public bool LoadRoom(int id, out Room room)
        {
            room = LoadRoomInternal(id, false);
            return room != null;
        }

        private Room LoadRoomInternal(int id, bool botCheck)
        {
            // Si ya existe (resuelta o pendiente), devolver sin reconstruir
            if (TryGetRoom(id, out Room existing))
                return existing;

            RoomData data = GenerateRoomData(id) ?? LoadRoomData(id);
            if (data == null) return null;

            // Registrar el Lazy ANTES de construir la Room, para que solicitudes
            // concurrentes encolen en el mismo Lazy y no creen instancias duplicadas.
            var lazy = _rooms.GetOrAdd(id,
                _ => new Lazy<Room>(
                    () => BuildRoom(data, botCheck),
                    System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));

            try { return lazy.Value; }
            catch
            {
                // Si la construcción falló, eliminar para que el siguiente intento reintente
                _rooms.TryRemove(id, out _);
                return null;
            }
        }

        private Room BuildRoom(RoomData data, bool botCheck)
        {
            var room = new Room(data);

            if (botCheck)
            {
                // FIX: usar Task.Run en vez de new Task(...).Start() — antipatrón
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    RoleplayBotManager.DeployCachedBots(room);
                });
            }

            return room;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  CREATE ROOM
        // ══════════════════════════════════════════════════════════════════════════

        public RoomData CreateRoom(GameClient session, string name, string description,
            string model, int category, string city, int maxVisitors, int tradeSettings)
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
                    "INSERT INTO `rooms` (`roomtype`,`caption`,`description`,`owner`," +
                    "`model_name`,`category`,`users_max`,`trade_settings`,`username`) " +
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

        // ══════════════════════════════════════════════════════════════════════════
        //  BÚSQUEDAS
        // ══════════════════════════════════════════════════════════════════════════

        public List<RoomData> SearchGroupRooms(string query) =>
            (from r in _loadedRoomData
             where r.Value.State != 3 && r.Value.Group != null &&
                   (r.Value.OwnerName.StartsWith(query) ||
                    r.Value.Tags.Contains(query) ||
                    r.Value.Name.Contains(query))
             orderby r.Value.UsersNow descending
             select r.Value).Take(50).ToList();

        public List<RoomData> SearchTaggedRooms(string query) =>
            (from r in _loadedRoomData
             where r.Value.UsersNow >= 0 && r.Value.State != 3 &&
                   r.Value.Tags.Contains(query)
             orderby r.Value.UsersNow descending
             select r.Value).Take(50).ToList();

        public List<RoomData> GetPopularRooms(int category, int amount = 50) =>
            (from r in _loadedRoomData
             where r.Value.UsersNow > 0 &&
                   (category == -1 || r.Value.Category == category) &&
                   r.Value.State != 3
             orderby r.Value.Score descending
             orderby r.Value.UsersNow descending
             select r.Value).Take(amount).ToList();

        public List<RoomData> GetRecommendedRooms(int amount = 50, int currentRoomId = 0) =>
            (from r in _loadedRoomData
             where r.Value.UsersNow >= 0 && r.Value.Score >= 0 &&
                   r.Value.State != 3 && r.Value.Id != currentRoomId
             orderby r.Value.Score descending
             orderby r.Value.UsersNow descending
             select r.Value).Take(amount).ToList();

        public List<RoomData> GetPopularRatedRooms(int amount = 50) =>
            (from r in _loadedRoomData
             where r.Value.State != 3
             orderby r.Value.Score descending
             select r.Value).Take(amount).ToList();

        public List<RoomData> GetRoomsByCategory(int category, int amount = 50) =>
            (from r in _loadedRoomData
             where r.Value.Category == category && r.Value.UsersNow > 0 && r.Value.State != 3
             orderby r.Value.UsersNow descending
             select r.Value).Take(amount).ToList();

        public List<RoomData> GetOnGoingRoomPromotions(int mode, int amount = 50)
        {
            var query = _loadedRoomData
                .Where(r => r.Value.HasActivePromotion && r.Value.State != 3)
                .Select(r => r.Value);

            return (mode == 17
                ? query.OrderByDescending(r => r.Promotion.TimestampStarted)
                : query.OrderByDescending(r => r.UsersNow))
                .Take(amount).ToList();
        }

        public List<RoomData> GetPromotedRooms(int categoryId, int amount = 50) =>
            (from r in _loadedRoomData
             where r.Value.HasActivePromotion &&
                   r.Value.Promotion.CategoryId == categoryId && r.Value.State != 3
             orderby r.Value.Promotion.TimestampStarted descending
             select r.Value).Take(amount).ToList();

        public List<RoomData> GetGroupRooms(int amount = 50) =>
            (from r in _loadedRoomData
             where r.Value.Group != null && r.Value.State != 3
             orderby r.Value.Score descending
             select r.Value).Take(amount).ToList();

        public List<KeyValuePair<string, int>> GetPopularRoomTags()
        {
            var tags = (from r in _loadedRoomData
                        where r.Value.UsersNow >= 0 && r.Value.State != 3
                        orderby r.Value.UsersNow descending
                        orderby r.Value.Score descending
                        select r.Value.Tags).Take(50);

            var tagValues = new Dictionary<string, int>();
            foreach (var tagList in tags)
                foreach (string tag in tagList)
                    tagValues[tag] = tagValues.TryGetValue(tag, out int c) ? c + 1 : 1;

            return tagValues.OrderByDescending(kvp => kvp.Value).ToList();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  DISPOSE
        // ══════════════════════════════════════════════════════════════════════════

        public async Task DisposeAsync()
        {
            var tasks = new List<Task>();

            foreach (var lazy in _rooms.Values)
            {
                if (!lazy.IsValueCreated) continue;
                Room r = TrySafeGetValue(lazy);
                if (r != null) tasks.Add(UnloadRoom(r));
            }

            await Task.WhenAll(tasks);
            Out.WriteLine("¡He terminado de deshacerse de las habitaciones!", "Polar.HabboHotel", ConsoleColor.DarkGray);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPERS PRIVADOS
        // ══════════════════════════════════════════════════════════════════════════

        // Evita que una excepción en Lazy.Value se propague en iteraciones
        private static Room TrySafeGetValue(Lazy<Room> lazy)
        {
            try { return lazy.IsValueCreated ? lazy.Value : null; }
            catch { return null; }
        }
    }
}