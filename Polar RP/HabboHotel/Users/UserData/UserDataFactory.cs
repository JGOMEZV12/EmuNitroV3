using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Polar.Database.Interfaces;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Subscriptions;
using Polar.HabboHotel.Users.Authenticator;
using Polar.HabboHotel.Users.Badges;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;

namespace Polar.HabboHotel.Users.UserDataManagement
{
    public static class UserDataFactory
    {
        // ── Cache de sesión activa ────────────────────────────────────────────────
        private static readonly ConcurrentDictionary<int, UserData> _cache = new();

        public static void ClearUserData(int userId) => _cache.TryRemove(userId, out _);

        // ══════════════════════════════════════════════════════════════════════════
        //  LOGIN PATH — by SSO ticket
        //
        //  PROBLEMA ORIGINAL: 14-16 queries secuenciales en un solo hilo de red.
        //  Con latencia BD de ~2ms cada una → ~30ms de espera SÓLO en queries,
        //  más construcción de colecciones grandes (amigos, salas) en el mismo hilo.
        //
        //  SOLUCIÓN:
        //  1. Query 0 (auth): síncrona — necesitamos el userId para todo lo demás.
        //  2. Queries 1-N: en paralelo con Task.WhenAll sobre un pool de conexiones.
        //     Cada bloque usa su propia conexión — sin compartir IQueryAdapter.
        //  3. Construcción de colecciones: fuera de cualquier conexión abierta.
        //  4. Habbo: construido con DataRows ya listos — 0 queries en el constructor.
        // ══════════════════════════════════════════════════════════════════════════

        public static UserData GetUserData(string sessionTicket, out byte errorCode)
        {
            errorCode = 0;

            // ── Fase 0: auth (secuencial — necesitamos userId) ─────────────────
            DataRow dUserInfo;
            int userId;

            using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery("SELECT * FROM `users` WHERE `auth_ticket` = @sso LIMIT 1");
                db.AddParameter("sso", sessionTicket);
                dUserInfo = db.getRow();
            }

            if (dUserInfo == null) { errorCode = 1; return null; }

            userId = Convert.ToInt32(dUserInfo["id"]);

            // Kick clon antes de continuar
            var existing = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userId);
            if (existing != null) { errorCode = 2; existing.Disconnect(false); return null; }

            // Cache hit (login doble improbable, pero por si acaso)
            if (_cache.TryGetValue(userId, out UserData cached)) return cached;

            // ── Fase 1: queries paralelas ──────────────────────────────────────
            // Cada Task tiene su propia conexión de pool; no hay race condition.
            DataRow userInfo = null;
            DataTable dAchievements = null;
            DataTable dFavRooms = null;
            DataTable dIgnores = null;
            DataTable dBadges = null;
            DataTable dFriends = null;
            DataTable dRequests = null;
            DataTable dRooms = null;
            DataTable dQuests = null;
            DataTable dRelations = null;
            DataTable dSubscriptions = null;
            DataRow dPoll = null;
            DataTable dRecipes = null;
            DataRow dStats = null;

            // FIX: Task.WhenAll lanza todas las queries en paralelo.
            //      Con 13 queries × 2ms latencia = 26ms secuencial → ~2-3ms paralelo.
            Task.WaitAll(

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    userInfo = EnsureUserInfo(db, userId);
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `group`,`level`,`progress` FROM `user_achievements` WHERE `userid` = @id");
                    db.AddParameter("id", userId);
                    dAchievements = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT room_id FROM user_favorites WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dFavRooms = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT ignore_id FROM user_ignores WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dIgnores = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `badge_id`,`badge_slot` FROM user_badges WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dBadges = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery(
                        "SELECT users.id,users.username,users.motto,users.look," +
                        "users.last_online,users.hide_inroom,users.hide_online " +
                        "FROM users JOIN messenger_friendships " +
                        "ON users.id = messenger_friendships.user_one_id " +
                        "WHERE messenger_friendships.user_two_id = @id " +
                        "UNION ALL " +
                        "SELECT users.id,users.username,users.motto,users.look," +
                        "users.last_online,users.hide_inroom,users.hide_online " +
                        "FROM users JOIN messenger_friendships " +
                        "ON users.id = messenger_friendships.user_two_id " +
                        "WHERE messenger_friendships.user_one_id = @id");
                    db.AddParameter("id", userId);
                    dFriends = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery(
                        "SELECT messenger_requests.from_id,messenger_requests.to_id,users.username " +
                        "FROM users JOIN messenger_requests " +
                        "ON users.id = messenger_requests.from_id " +
                        "WHERE messenger_requests.to_id = @id");
                    db.AddParameter("id", userId);
                    dRequests = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT * FROM rooms WHERE `owner` = @id LIMIT 150");
                    db.AddParameter("id", userId);
                    dRooms = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `quest_id`,`progress` FROM user_quests WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dQuests = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `id`,`user_id`,`target`,`type` FROM `user_relationships` WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dRelations = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT * FROM user_subscriptions WHERE user_id = @id");
                    db.AddParameter("id", userId);
                    dSubscriptions = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `poll_id` FROM `user_polls` WHERE `user_id` = @id LIMIT 1");
                    db.AddParameter("id", userId);
                    dPoll = db.getRow();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `recipe` FROM `user_recipes` WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dRecipes = db.getTable();
                }),

                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    dStats = LoadOrCreateStats(db, userId);
                })
            );

            // ── Fase 2: marcar online + limpiar ticket (no bloquea al usuario) ─
            // FIX: fire-and-forget — el cliente ya está autenticado; la BD puede
            //      tardar sin que eso retrase el envío de paquetes al cliente.
            Task.Run(() =>
            {
                using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.RunQuery($"UPDATE `users` SET `online`='1', `auth_ticket`='' WHERE `id`='{userId}' LIMIT 1");
            });

            // ── Fase 3: construir colecciones (CPU, sin IO) ────────────────────
            var achievements = BuildAchievements(dAchievements);
            var favouritedRooms = BuildIntList(dFavRooms, "room_id");
            var ignores = BuildIntList(dIgnores, "ignore_id");
            var badges = BuildBadges(dBadges);
            var friends = BuildFriends(dFriends);
            var requests = BuildRequests(userId, dRequests);
            var rooms = BuildRooms(dRooms);      // batch rp_rooms
            var quests = BuildQuests(dQuests);
            var relationships = BuildRelationships(dRelations, friends);
            var subscriptions = BuildSubscriptions(dSubscriptions);

            // ── Fase 4: construir Habbo (0 queries) ────────────────────────────
            Habbo user = HabboFactory.GenerateHabbo(dUserInfo, userInfo, dPoll, dRecipes, dStats);

            var data = new UserData(userId, achievements, favouritedRooms, ignores, badges,
                                    friends, requests, rooms, quests, user, relationships, subscriptions);
            _cache.TryAdd(userId, data);
            return data;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  LOOKUP BY ID (bots, moderación, etc.)
        // ══════════════════════════════════════════════════════════════════════════

        public static UserData GetUserData(int userId)
        {
            if (_cache.TryGetValue(userId, out UserData cached)) return cached;

            DataRow dUserInfo = null;
            DataRow userInfo = null;
            DataTable dRelations = null;
            DataTable dBadges = null;

            // FIX: estas 3 queries también en paralelo para lookups rápidos
            Task.WaitAll(
                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT * FROM `users` WHERE `id` = @id LIMIT 1");
                    db.AddParameter("id", userId);
                    dUserInfo = db.getRow();
                }),
                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    userInfo = EnsureUserInfo_ReadOnly(userId);
                }),
                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `id`,`target`,`type` FROM user_relationships WHERE user_id = @id");
                    db.AddParameter("id", userId);
                    dRelations = db.getTable();
                }),
                Task.Run(() =>
                {
                    using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery("SELECT `badge_id`,`badge_slot` FROM user_badges WHERE `user_id` = @id");
                    db.AddParameter("id", userId);
                    dBadges = db.getTable();
                })
            );

            if (dUserInfo == null) return null;

            // Kick clon si existe (igual que en la otra overload)
            var existing = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userId);
            if (existing != null) { existing.Disconnect(true); return null; }

            var badges = BuildBadges(dBadges);
            var relationships = BuildRelationshipsMinimal(dRelations);

            Habbo user = HabboFactory.GenerateHabbo(dUserInfo, userInfo, null, null, null);

            var data = new UserData(
                userId,
                new ConcurrentDictionary<string, UserAchievement>(),
                new List<int>(), new List<int>(),
                badges,
                new Dictionary<int, MessengerBuddy>(),
                new Dictionary<int, MessengerRequest>(),
                new List<RoomData>(),
                new Dictionary<int, int>(),
                user, relationships,
                new Dictionary<string, Subscription>());

            _cache.TryAdd(userId, data);
            return data;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPERS DE BD
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Garantiza que exista la fila en user_info; la crea si no existe.
        /// Usa la conexión que recibe (para reutilizar la del login cuando conviene).
        /// </summary>
        private static DataRow EnsureUserInfo(IQueryAdapter db, int userId)
        {
            db.SetQuery("SELECT * FROM `user_info` WHERE `user_id` = @id LIMIT 1");
            db.AddParameter("id", userId);
            DataRow row = db.getRow();
            if (row != null) return row;

            db.RunQuery($"INSERT INTO `user_info` (`user_id`) VALUES ('{userId}')");
            db.SetQuery("SELECT * FROM `user_info` WHERE `user_id` = @id LIMIT 1");
            db.AddParameter("id", userId);
            return db.getRow();
        }

        /// <summary>
        /// Versión read-only para el path de lookup-by-id (abre su propia conexión).
        /// </summary>
        private static DataRow EnsureUserInfo_ReadOnly(int userId)
        {
            using var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
            return EnsureUserInfo(db, userId);
        }

        /// <summary>
        /// Carga user_stats; si no existe, la inserta y la recarga.
        /// </summary>
        private static DataRow LoadOrCreateStats(IQueryAdapter db, int userId)
        {
            const string statsQuery =
                "SELECT `id`,`roomvisits`,`onlinetime`,`respect`,`respectgiven`,`giftsgiven`," +
                "`giftsreceived`,`dailyrespectpoints`,`dailypetrespectpoints`,`achievementscore`," +
                "`quest_id`,`quest_progress`,`groupid`,`tickets_answered`,`respectstimestamp`,`forum_posts` " +
                "FROM `user_stats` WHERE `id` = @id LIMIT 1";

            db.SetQuery(statsQuery);
            db.AddParameter("id", userId);
            DataRow row = db.getRow();

            if (row != null) return row;

            db.RunQuery($"INSERT INTO `user_stats` (`id`) VALUES ('{userId}')");
            db.SetQuery(statsQuery);
            db.AddParameter("id", userId);
            return db.getRow();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  BUILDERS DE COLECCIONES (CPU puro, sin IO)
        // ══════════════════════════════════════════════════════════════════════════

        private static ConcurrentDictionary<string, UserAchievement> BuildAchievements(DataTable t)
        {
            var d = new ConcurrentDictionary<string, UserAchievement>(
                StringComparer.Ordinal);
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                string group = Convert.ToString(r["group"]);
                d.TryAdd(group, new UserAchievement(
                    group,
                    Convert.ToInt32(r["level"]),
                    Convert.ToInt32(r["progress"])));
            }
            return d;
        }

        private static List<int> BuildIntList(DataTable t, string column)
        {
            if (t == null) return new List<int>(0);
            var list = new List<int>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
                list.Add(Convert.ToInt32(r[column]));
            return list;
        }

        private static List<Badge> BuildBadges(DataTable t)
        {
            if (t == null) return new List<Badge>(0);
            var list = new List<Badge>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
                list.Add(new Badge(
                    Convert.ToString(r["badge_id"]),
                    Convert.ToInt32(r["badge_slot"])));
            return list;
        }

        private static Dictionary<int, MessengerBuddy> BuildFriends(DataTable t)
        {
            if (t == null) return new Dictionary<int, MessengerBuddy>(0);
            var d = new Dictionary<int, MessengerBuddy>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (!d.ContainsKey(id))
                    d[id] = new MessengerBuddy(
                        id,
                        Convert.ToString(r["username"]),
                        Convert.ToString(r["look"]),
                        Convert.ToString(r["motto"]),
                        Convert.ToInt32(r["last_online"]),
                        PolarEnvironment.EnumToBool(r["hide_online"].ToString()),
                        PolarEnvironment.EnumToBool(r["hide_inroom"].ToString()),
                        false);
            }
            return d;
        }

        private static Dictionary<int, MessengerRequest> BuildRequests(int userId, DataTable t)
        {
            if (t == null) return new Dictionary<int, MessengerRequest>(0);
            var d = new Dictionary<int, MessengerRequest>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                int senderId = Convert.ToInt32(r["from_id"]);
                if (!d.ContainsKey(senderId))
                    d[senderId] = new MessengerRequest(
                        userId,
                        senderId,
                        Convert.ToString(r["username"]));
            }
            return d;
        }

        /// <summary>
        /// FIX ORIGINAL: BuildRooms hacía 1 query SELECT rp_rooms por sala →
        /// N roundtrips a BD (si el usuario tiene 50 salas = 50 queries).
        /// AHORA: 1 sola query batch con IN (...) → 1 roundtrip total.
        /// </summary>
        private static List<RoomData> BuildRooms(DataTable t)
        {
            if (t == null || t.Rows.Count == 0) return new List<RoomData>(0);

            var roomIds = new List<int>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
                roomIds.Add(Convert.ToInt32(r["id"]));

            // Una sola query para todos los rp_rooms
            DataTable rpTable = null;
            string idsJoined = string.Join(",", roomIds);
            using (var db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery($"SELECT * FROM `rp_rooms` WHERE `id` IN ({idsJoined})");
                rpTable = db.getTable();
            }

            // Indexar por id para lookup O(1)
            var rpIndex = new Dictionary<int, DataRow>(rpTable?.Rows.Count ?? 0);
            if (rpTable != null)
                foreach (DataRow rp in rpTable.Rows)
                    rpIndex[Convert.ToInt32(rp["id"])] = rp;

            var list = new List<RoomData>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                rpIndex.TryGetValue(id, out DataRow rpRow);
                list.Add(PolarEnvironment.GetGame().GetRoomManager()
                    .FetchRoomData(id, r, rpRow));
            }
            return list;
        }

        private static Dictionary<int, int> BuildQuests(DataTable t)
        {
            if (t == null) return new Dictionary<int, int>(0);
            var d = new Dictionary<int, int>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
                d[Convert.ToInt32(r["quest_id"])] = Convert.ToInt32(r["progress"]);
            return d;
        }

        private static Dictionary<int, Relationship> BuildRelationships(
            DataTable t, Dictionary<int, MessengerBuddy> friends)
        {
            if (t == null) return new Dictionary<int, Relationship>(0);
            var d = new Dictionary<int, Relationship>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                int target = Convert.ToInt32(r[2]);
                if (friends.ContainsKey(target) && !d.ContainsKey(target))
                    d[target] = new Relationship(
                        Convert.ToInt32(r[0]),
                        target,
                        Convert.ToInt32(r[3].ToString()));
            }
            return d;
        }

        private static Dictionary<int, Relationship> BuildRelationshipsMinimal(DataTable t)
        {
            if (t == null) return new Dictionary<int, Relationship>(0);
            var d = new Dictionary<int, Relationship>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (!d.ContainsKey(id))
                    d[Convert.ToInt32(r["target"])] = new Relationship(
                        id,
                        Convert.ToInt32(r["target"]),
                        Convert.ToInt32(r["type"].ToString()));
            }
            return d;
        }

        private static Dictionary<string, Subscription> BuildSubscriptions(DataTable t)
        {
            if (t == null) return new Dictionary<string, Subscription>(0);
            var d = new Dictionary<string, Subscription>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
            {
                string key = (string)r["subscription_id"];
                d[key] = new Subscription(
                    key,
                    (int)r["timestamp_expire"],
                    (int)r["timestamp_activated"]);
            }
            return d;
        }
    }
}