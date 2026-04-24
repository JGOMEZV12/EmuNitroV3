using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users.Badges;
using Polar.HabboHotel.Achievements;
using Polar.HabboHotel.Users.Messenger;
using Polar.HabboHotel.Users.Relationships;
using Polar.HabboHotel.Users.Authenticator;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Subscriptions;

namespace Polar.HabboHotel.Users.UserDataManagement
{
    public static class UserDataFactory
    {
        private static readonly ConcurrentDictionary<int, UserData> _cache = new();

        public static void ClearUserData(int userId) => _cache.TryRemove(userId, out _);

        // ─── Login-path (by SSO ticket) ──────────────────────────────────────────

        public static UserData GetUserData(string sessionTicket, out byte errorCode)
        {
            errorCode = 0;

            DataRow dUserInfo, userInfo;
            int userId;

            DataTable dAchievements, dFavouriteRooms, dIgnores, dBadges;
            DataTable dFriends, dRequests, dRooms, dQuests, dRelations, dSubscriptions;

            // Datos que antes se cargaban dentro del constructor de Habbo
            DataRow dPoll;
            DataTable dRecipes;
            DataRow dStats;

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery("SELECT * FROM `users` WHERE `auth_ticket` = @sso LIMIT 1");
                db.AddParameter("sso", sessionTicket);
                dUserInfo = db.getRow();

                if (dUserInfo == null) { errorCode = 1; return null; }

                userId = Convert.ToInt32(dUserInfo["id"]);

                // Kick duplicate session
                var existing = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userId);
                if (existing != null) { errorCode = 2; existing.Disconnect(false); return null; }

                if (_cache.TryGetValue(userId, out UserData cached)) return cached;

                // ── Achievements ────────────────────────────────────────────────
                db.SetQuery("SELECT `group`,`level`,`progress` FROM `user_achievements` WHERE `userid` = @id");
                db.AddParameter("id", userId);
                dAchievements = db.getTable();

                // ── Favourite rooms ─────────────────────────────────────────────
                db.SetQuery("SELECT room_id FROM user_favorites WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dFavouriteRooms = db.getTable();

                // ── Ignores ─────────────────────────────────────────────────────
                db.SetQuery("SELECT ignore_id FROM user_ignores WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dIgnores = db.getTable();

                // ── Badges ──────────────────────────────────────────────────────
                db.SetQuery("SELECT `badge_id`,`badge_slot` FROM user_badges WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dBadges = db.getTable();

                // ── Friends ─────────────────────────────────────────────────────
                db.SetQuery(
                    "SELECT users.id,users.username,users.motto,users.look,users.last_online,users.hide_inroom,users.hide_online " +
                    "FROM users JOIN messenger_friendships ON users.id = messenger_friendships.user_one_id WHERE messenger_friendships.user_two_id = @id " +
                    "UNION ALL " +
                    "SELECT users.id,users.username,users.motto,users.look,users.last_online,users.hide_inroom,users.hide_online " +
                    "FROM users JOIN messenger_friendships ON users.id = messenger_friendships.user_two_id WHERE messenger_friendships.user_one_id = @id");
                db.AddParameter("id", userId);
                dFriends = db.getTable();

                // ── Messenger requests ──────────────────────────────────────────
                db.SetQuery(
                    "SELECT messenger_requests.from_id,messenger_requests.to_id,users.username " +
                    "FROM users JOIN messenger_requests ON users.id = messenger_requests.from_id " +
                    "WHERE messenger_requests.to_id = @id");
                db.AddParameter("id", userId);
                dRequests = db.getTable();

                // ── Rooms ───────────────────────────────────────────────────────
                db.SetQuery("SELECT * FROM rooms WHERE `owner` = @id LIMIT 150");
                db.AddParameter("id", userId);
                dRooms = db.getTable();

                // ── Quests ──────────────────────────────────────────────────────
                db.SetQuery("SELECT `quest_id`,`progress` FROM user_quests WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dQuests = db.getTable();

                // ── Relationships ───────────────────────────────────────────────
                db.SetQuery("SELECT `id`,`user_id`,`target`,`type` FROM `user_relationships` WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dRelations = db.getTable();

                // ── Subscriptions ───────────────────────────────────────────────
                db.SetQuery("SELECT * FROM user_subscriptions WHERE user_id = @id");
                db.AddParameter("id", userId);
                dSubscriptions = db.getTable();

                // ── user_info (con insert si no existe) ─────────────────────────
                userInfo = EnsureUserInfo(db, userId);

                // ── Polls (antes en constructor de Habbo) ───────────────────────
                db.SetQuery("SELECT `poll_id` FROM `user_polls` WHERE `user_id` = @id LIMIT 1");
                db.AddParameter("id", userId);
                dPoll = db.getRow();

                // ── Recipes (antes en constructor de Habbo) ─────────────────────
                db.SetQuery("SELECT `recipe` FROM `user_recipes` WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dRecipes = db.getTable();

                // ── Stats (antes en constructor de Habbo) ───────────────────────
                db.SetQuery(
                    "SELECT `id`,`roomvisits`,`onlinetime`,`respect`,`respectgiven`,`giftsgiven`," +
                    "`giftsreceived`,`dailyrespectpoints`,`dailypetrespectpoints`,`achievementscore`," +
                    "`quest_id`,`quest_progress`,`groupid`,`tickets_answered`,`respectstimestamp`,`forum_posts` " +
                    "FROM `user_stats` WHERE `id` = @id LIMIT 1");
                db.AddParameter("id", userId);
                dStats = db.getRow();

                if (dStats == null)
                {
                    db.RunQuery($"INSERT INTO `user_stats` (`id`) VALUES ('{userId}')");
                    db.SetQuery(
                        "SELECT `id`,`roomvisits`,`onlinetime`,`respect`,`respectgiven`,`giftsgiven`," +
                        "`giftsreceived`,`dailyrespectpoints`,`dailypetrespectpoints`,`achievementscore`," +
                        "`quest_id`,`quest_progress`,`groupid`,`tickets_answered`,`respectstimestamp`,`forum_posts` " +
                        "FROM `user_stats` WHERE `id` = @id LIMIT 1");
                    db.AddParameter("id", userId);
                    dStats = db.getRow();
                }

                // ── Marcar online, limpiar ticket ───────────────────────────────
                db.RunQuery($"UPDATE `users` SET `online` = '1', `auth_ticket` = '' WHERE `id` = '{userId}' LIMIT 1");
            }

            // ── Construir colecciones fuera de la conexión ───────────────────────
            var achievements = BuildAchievements(dAchievements);
            var favouritedRooms = BuildIntList(dFavouriteRooms, "room_id");
            var ignores = BuildIntList(dIgnores, "ignore_id");
            var badges = BuildBadges(dBadges);
            var friends = BuildFriends(dFriends);
            var requests = BuildRequests(userId, dRequests);
            var rooms = BuildRooms(dRooms);           // ahora batch
            var quests = BuildQuests(dQuests);
            var relationships = BuildRelationships(dRelations, friends);
            var subscriptions = BuildSubscriptions(dSubscriptions);

            // Habbo se construye con los DataRows precargados — 0 queries en el constructor
            Habbo user = HabboFactory.GenerateHabbo(dUserInfo, userInfo, dPoll, dRecipes, dStats);

            var data = new UserData(userId, achievements, favouritedRooms, ignores, badges,
                                    friends, requests, rooms, quests, user, relationships, subscriptions);
            _cache.TryAdd(userId, data);
            return data;
        }

        // ─── Look-up by ID ───────────────────────────────────────────────────────

        public static UserData GetUserData(int userId)
        {
            if (_cache.TryGetValue(userId, out UserData cached)) return cached;

            DataRow dUserInfo, userInfo;
            DataTable dRelations, dBadges;

            using (IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                db.SetQuery("SELECT * FROM `users` WHERE `id` = @id LIMIT 1");
                db.AddParameter("id", userId);
                dUserInfo = db.getRow();
                if (dUserInfo == null) return null;

                var existing = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(userId);
                if (existing != null) { existing.Disconnect(true); return null; }

                userInfo = EnsureUserInfo(db, userId);

                db.SetQuery("SELECT `id`,`target`,`type` FROM user_relationships WHERE user_id = @id");
                db.AddParameter("id", userId);
                dRelations = db.getTable();

                db.SetQuery("SELECT `badge_id`,`badge_slot` FROM user_badges WHERE `user_id` = @id");
                db.AddParameter("id", userId);
                dBadges = db.getTable();
            }

            var badges = BuildBadges(dBadges);
            var relationships = BuildRelationshipsMinimal(dRelations);

            // Para lookups por ID no necesitamos stats/polls/recipes
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

        // ─── Shared DB helper ────────────────────────────────────────────────────

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

        // ─── Collection builders ─────────────────────────────────────────────────

        private static ConcurrentDictionary<string, UserAchievement> BuildAchievements(DataTable t)
        {
            var d = new ConcurrentDictionary<string, UserAchievement>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                string group = Convert.ToString(r["group"]);
                d.TryAdd(group, new UserAchievement(group, Convert.ToInt32(r["level"]), Convert.ToInt32(r["progress"])));
            }
            return d;
        }

        private static List<int> BuildIntList(DataTable t, string column)
        {
            var list = new List<int>();
            if (t == null) return list;
            foreach (DataRow r in t.Rows)
                list.Add(Convert.ToInt32(r[column]));
            return list;
        }

        private static List<Badge> BuildBadges(DataTable t)
        {
            var list = new List<Badge>();
            if (t == null) return list;
            foreach (DataRow r in t.Rows)
                list.Add(new Badge(Convert.ToString(r["badge_id"]), Convert.ToInt32(r["badge_slot"])));
            return list;
        }

        private static Dictionary<int, MessengerBuddy> BuildFriends(DataTable t)
        {
            var d = new Dictionary<int, MessengerBuddy>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (!d.ContainsKey(id))
                    d[id] = new MessengerBuddy(id,
                        Convert.ToString(r["username"]), Convert.ToString(r["look"]),
                        Convert.ToString(r["motto"]), Convert.ToInt32(r["last_online"]),
                        PolarEnvironment.EnumToBool(r["hide_online"].ToString()),
                        PolarEnvironment.EnumToBool(r["hide_inroom"].ToString()), false);
            }
            return d;
        }

        private static Dictionary<int, MessengerRequest> BuildRequests(int userId, DataTable t)
        {
            var d = new Dictionary<int, MessengerRequest>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                int senderId = Convert.ToInt32(r["from_id"]);
                if (!d.ContainsKey(senderId))
                    d[senderId] = new MessengerRequest(userId, senderId, Convert.ToString(r["username"]));
            }
            return d;
        }

        /// <summary>
        /// ANTES: 1 query SELECT rp_rooms por sala → N roundtrips a BD.
        /// AHORA: 1 sola query batch con IN (...) → 1 roundtrip total.
        /// </summary>
        private static List<RoomData> BuildRooms(DataTable t)
        {
            var list = new List<RoomData>();
            if (t == null || t.Rows.Count == 0) return list;

            // Recoger todos los IDs de una vez
            var roomIds = new List<int>(t.Rows.Count);
            foreach (DataRow r in t.Rows)
                roomIds.Add(Convert.ToInt32(r["id"]));

            // UNA sola query para todos los rp_rooms
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
            var d = new Dictionary<int, int>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
                d[Convert.ToInt32(r["quest_id"])] = Convert.ToInt32(r["progress"]);
            return d;
        }

        private static Dictionary<int, Relationship> BuildRelationships(DataTable t, Dictionary<int, MessengerBuddy> friends)
        {
            var d = new Dictionary<int, Relationship>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                int target = Convert.ToInt32(r[2]);
                if (friends.ContainsKey(target) && !d.ContainsKey(target))
                    d[target] = new Relationship(Convert.ToInt32(r[0]), target, Convert.ToInt32(r[3].ToString()));
            }
            return d;
        }

        private static Dictionary<int, Relationship> BuildRelationshipsMinimal(DataTable t)
        {
            var d = new Dictionary<int, Relationship>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (!d.ContainsKey(id))
                    d[Convert.ToInt32(r["target"])] = new Relationship(id, Convert.ToInt32(r["target"]), Convert.ToInt32(r["type"].ToString()));
            }
            return d;
        }

        private static Dictionary<string, Subscription> BuildSubscriptions(DataTable t)
        {
            var d = new Dictionary<string, Subscription>();
            if (t == null) return d;
            foreach (DataRow r in t.Rows)
            {
                string key = (string)r["subscription_id"];
                d[key] = new Subscription(key, (int)r["timestamp_expire"], (int)r["timestamp_activated"]);
            }
            return d;
        }
    }
}