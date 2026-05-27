using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired
{
    public static class WiredVariableReferenceSupport
    {
        public const int TARGET_USER = 0;
        public const int TARGET_ROOM = 3;
        public const int SHARED_AVAILABILITY = 11;

        // Cache: "roomId:itemId:userId" → CachedUserAssignment
        private static readonly ConcurrentDictionary<string, CachedUserAssignment> USER_ASSIGNMENT_CACHE = new();
        // Cache: "roomId:itemId" → CachedRoomAssignment
        private static readonly ConcurrentDictionary<string, CachedRoomAssignment> ROOM_ASSIGNMENT_CACHE = new();

        public static bool IsSharedAvailability(int availability) => availability == SHARED_AVAILABILITY;

        // ── FindSharedDefinition ─────────────────────────────────────────────
        public static SharedDefinitionOption FindSharedDefinition(Room room, int sourceRoomId, int sourceVariableItemId, int sourceTargetType)
        {
            if (room == null || sourceRoomId <= 0 || sourceVariableItemId <= 0) return null;

            foreach (var option in LoadRoomOptions(room))
            {
                if (option.RoomId != sourceRoomId) continue;
                foreach (var def in option.Variables)
                {
                    if (def.ItemId == sourceVariableItemId && def.TargetType == sourceTargetType)
                        return def;
                }
            }
            return null;
        }

        // ── LoadRoomOptions ──────────────────────────────────────────────────
        public static List<RoomOption> LoadRoomOptions(Room room)
        {
            if (room == null || room.OwnerId <= 0) return new List<RoomOption>();

            var optionsByRoomId = new Dictionary<int, RoomOption>();

            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery(@"
                    SELECT rooms.id AS room_id, rooms.caption AS room_name,
                           items.id AS base_item, items.wired_data, items_base.interaction_type
                    FROM rooms
                    INNER JOIN items ON rooms.id = items.room_id
                    INNER JOIN items_base ON items.base_item = items_base.id
                    WHERE rooms.owner = @ownerId AND rooms.id <> @roomId
                      AND items_base.interaction_type IN ('wf_var_user', 'wf_var_room')
                    ORDER BY rooms.caption ASC, items.id ASC");
                dbClient.AddParameter("ownerId", room.OwnerId);
                dbClient.AddParameter("roomId", room.Id);

                var table = dbClient.getTable();
                if (table == null) return new List<RoomOption>();

                foreach (System.Data.DataRow row in table.Rows)
                {
                    var def = ParseSharedDefinition(
                        Convert.ToString(row["interaction_type"]),
                        Convert.ToInt32(row["item_id"]),
                        Convert.ToString(row["wired_data"]),
                        Convert.ToInt32(row["room_id"]),
                        Convert.ToString(row["room_name"]));

                    if (def == null) continue;

                    if (!optionsByRoomId.TryGetValue(def.RoomId, out var option))
                    {
                        option = new RoomOption(def.RoomId, def.RoomName, new List<SharedDefinitionOption>());
                        optionsByRoomId[def.RoomId] = option;
                    }
                    option.Variables.Add(def);
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[WiredVariableReferenceSupport] LoadRoomOptions: {ex}");
            }

            var result = optionsByRoomId.Values.ToList();
            foreach (var opt in result)
                opt.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            result.Sort((a, b) => string.Compare(a.RoomName, b.RoomName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        // ── Shared User Assignment ───────────────────────────────────────────
        public static SharedUserAssignment GetSharedUserAssignment(AddonVariableReferenceBox reference, int userId)
        {
            if (reference == null || !reference.IsUserReference || userId <= 0) return null;

            string key = CreateUserCacheKey(reference.SourceRoomId, reference.SourceVariableItemId, userId);

            if (USER_ASSIGNMENT_CACHE.TryGetValue(key, out var cached))
                return cached.Present ? cached.Assignment : null;

            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT value, created_at, updated_at FROM room_user_wired_variables WHERE room_id = @roomId AND user_id = @userId AND variable_item_id = @itemId LIMIT 1");
                dbClient.AddParameter("roomId", reference.SourceRoomId);
                dbClient.AddParameter("userId", userId);
                dbClient.AddParameter("itemId", reference.SourceVariableItemId);

                var row = dbClient.getRow();
                if (row == null)
                {
                    USER_ASSIGNMENT_CACHE[key] = CachedUserAssignment.Missing();
                    return null;
                }

                int? value = row.IsNull("value") ? (int?)null : Convert.ToInt32(row["value"]);
                int createdAt = NormalizeTimestamp(Convert.ToInt32(row["created_at"]), 0);
                var assignment = new SharedUserAssignment(value, createdAt, NormalizeTimestamp(Convert.ToInt32(row["updated_at"]), createdAt));
                USER_ASSIGNMENT_CACHE[key] = CachedUserAssignment.FromAssignment(assignment);
                return assignment;
            }
            catch (Exception ex)
            {
                Logging.LogException($"[WiredVariableReferenceSupport] GetSharedUserAssignment: {ex}");
                return null;
            }
        }

        public static bool AssignSharedUserVariable(AddonVariableReferenceBox reference, int userId, int? value, bool overrideExisting)
        {
            if (reference == null || !reference.IsUserReference || reference.IsReadOnly || userId <= 0) return false;

            int? normalizedValue = reference.HasValue ? value : null;
            var existing = GetSharedUserAssignment(reference, userId);

            if (existing != null && !overrideExisting) return false;

            int now = PolarEnvironment.GetIUnixTimestamp();
            bool overwritten = existing != null && overrideExisting;

            if (!overwritten && existing != null && existing.Value == normalizedValue) return false;

            var next = (existing == null || overwritten)
                ? new SharedUserAssignment(normalizedValue, now, now)
                : new SharedUserAssignment(normalizedValue, existing.CreatedAt, existing.Value == normalizedValue ? existing.UpdatedAt : now);

            UpsertSharedUserAssignment(reference.SourceRoomId, reference.SourceVariableItemId, userId, next);
            USER_ASSIGNMENT_CACHE[CreateUserCacheKey(reference.SourceRoomId, reference.SourceVariableItemId, userId)] = CachedUserAssignment.FromAssignment(next);
            return true;
        }

        public static bool UpdateSharedUserVariable(AddonVariableReferenceBox reference, int userId, int? value)
        {
            if (reference == null || !reference.IsUserReference || reference.IsReadOnly || userId <= 0 || !reference.HasValue) return false;

            var existing = GetSharedUserAssignment(reference, userId);
            if (existing == null || existing.Value == value) return false;

            var next = new SharedUserAssignment(value, existing.CreatedAt, PolarEnvironment.GetIUnixTimestamp());
            UpsertSharedUserAssignment(reference.SourceRoomId, reference.SourceVariableItemId, userId, next);
            USER_ASSIGNMENT_CACHE[CreateUserCacheKey(reference.SourceRoomId, reference.SourceVariableItemId, userId)] = CachedUserAssignment.FromAssignment(next);
            return true;
        }

        public static bool RemoveSharedUserVariable(AddonVariableReferenceBox reference, int userId)
        {
            if (reference == null || !reference.IsUserReference || reference.IsReadOnly || userId <= 0) return false;

            var existing = GetSharedUserAssignment(reference, userId);
            if (existing == null) return false;

            DeleteSharedUserAssignment(reference.SourceRoomId, reference.SourceVariableItemId, userId);
            USER_ASSIGNMENT_CACHE[CreateUserCacheKey(reference.SourceRoomId, reference.SourceVariableItemId, userId)] = CachedUserAssignment.Missing();
            return true;
        }

        public static void CacheSharedUserAssignment(int sourceRoomId, int sourceVariableItemId, int userId, int? value, int createdAt, int updatedAt)
        {
            USER_ASSIGNMENT_CACHE[CreateUserCacheKey(sourceRoomId, sourceVariableItemId, userId)] =
                CachedUserAssignment.FromAssignment(new SharedUserAssignment(value, createdAt, updatedAt));
        }

        public static void ClearSharedUserAssignment(int sourceRoomId, int sourceVariableItemId, int userId)
        {
            USER_ASSIGNMENT_CACHE[CreateUserCacheKey(sourceRoomId, sourceVariableItemId, userId)] = CachedUserAssignment.Missing();
        }

        public static void ClearSharedUserDefinition(int sourceRoomId, int sourceVariableItemId)
        {
            string prefix = CreateDefinitionPrefix(sourceRoomId, sourceVariableItemId) + ":";
            foreach (var key in USER_ASSIGNMENT_CACHE.Keys.Where(k => k.StartsWith(prefix)).ToList())
                USER_ASSIGNMENT_CACHE.TryRemove(key, out _);
        }

        // ── Shared Room Assignment ───────────────────────────────────────────
        public static SharedRoomAssignment GetSharedRoomAssignment(AddonVariableReferenceBox reference)
        {
            if (reference == null || !reference.IsRoomReference) return null;

            string key = CreateRoomCacheKey(reference.SourceRoomId, reference.SourceVariableItemId);

            if (ROOM_ASSIGNMENT_CACHE.TryGetValue(key, out var cached))
                return cached.Present ? cached.Assignment : null;

            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT value, updated_at FROM room_wired_variables WHERE room_id = @roomId AND variable_item_id = @itemId LIMIT 1");
                dbClient.AddParameter("roomId", reference.SourceRoomId);
                dbClient.AddParameter("itemId", reference.SourceVariableItemId);

                var row = dbClient.getRow();
                if (row == null)
                {
                    ROOM_ASSIGNMENT_CACHE[key] = CachedRoomAssignment.Missing();
                    return null;
                }

                var assignment = new SharedRoomAssignment(Convert.ToInt32(row["value"]), NormalizeTimestamp(Convert.ToInt32(row["updated_at"]), 0));
                ROOM_ASSIGNMENT_CACHE[key] = CachedRoomAssignment.FromAssignment(assignment);
                return assignment;
            }
            catch (Exception ex)
            {
                Logging.LogException($"[WiredVariableReferenceSupport] GetSharedRoomAssignment: {ex}");
                return null;
            }
        }

        public static void CacheSharedRoomAssignment(int sourceRoomId, int sourceVariableItemId, int value, int updatedAt)
        {
            ROOM_ASSIGNMENT_CACHE[CreateRoomCacheKey(sourceRoomId, sourceVariableItemId)] =
                CachedRoomAssignment.FromAssignment(new SharedRoomAssignment(value, updatedAt));
        }

        public static void ClearSharedRoomDefinition(int sourceRoomId, int sourceVariableItemId)
        {
            ROOM_ASSIGNMENT_CACHE[CreateRoomCacheKey(sourceRoomId, sourceVariableItemId)] = CachedRoomAssignment.Missing();
        }

        // ── DB helpers ───────────────────────────────────────────────────────
        private static void UpsertSharedUserAssignment(int roomId, int itemId, int userId, SharedUserAssignment a)
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("INSERT INTO room_user_wired_variables (room_id, user_id, variable_item_id, value, created_at, updated_at) VALUES (@r, @u, @i, @v, @ca, @ua) ON DUPLICATE KEY UPDATE value = VALUES(value), updated_at = VALUES(updated_at)");
                dbClient.AddParameter("r", roomId);
                dbClient.AddParameter("u", userId);
                dbClient.AddParameter("i", itemId);
                dbClient.AddParameter("v", a.Value.HasValue ? (object)a.Value.Value : DBNull.Value);
                dbClient.AddParameter("ca", a.CreatedAt);
                dbClient.AddParameter("ua", a.UpdatedAt);
                dbClient.RunQuery();
            }
            catch (Exception ex) { Logging.LogException($"[WiredVariableReferenceSupport] UpsertSharedUserAssignment: {ex}"); }
        }

        private static void DeleteSharedUserAssignment(int roomId, int itemId, int userId)
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("DELETE FROM room_user_wired_variables WHERE room_id = @r AND user_id = @u AND variable_item_id = @i");
                dbClient.AddParameter("r", roomId);
                dbClient.AddParameter("u", userId);
                dbClient.AddParameter("i", itemId);
                dbClient.RunQuery();
            }
            catch (Exception ex) { Logging.LogException($"[WiredVariableReferenceSupport] DeleteSharedUserAssignment: {ex}"); }
        }

        // ── Parse helpers ────────────────────────────────────────────────────
        private static SharedDefinitionOption ParseSharedDefinition(string interactionType, int itemId, string wiredData, int roomId, string roomName)
        {
            if (interactionType == "wf_var_user")
            {
                var data = ParseUserDefinitionData(wiredData);
                if (data == null || !IsSharedAvailability(data.availability) || string.IsNullOrEmpty(data.variableName))
                    return null;
                return new SharedDefinitionOption(roomId, roomName, itemId, data.variableName, TARGET_USER, data.hasValue);
            }

            if (interactionType == "wf_var_room")
            {
                var data = ParseRoomDefinitionData(wiredData);
                if (data == null || !IsSharedAvailability(data.availability) || string.IsNullOrEmpty(data.variableName))
                    return null;
                return new SharedDefinitionOption(roomId, roomName, itemId, data.variableName, TARGET_ROOM, true);
            }

            return null;
        }

        private static UserDefinitionData ParseUserDefinitionData(string wiredData)
        {
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return null;
            var data = JsonConvert.DeserializeObject<UserDefinitionData>(wiredData);
            if (data == null) return null;
            data.variableName = WiredVariableNameValidator.NormalizeLegacy(data.variableName ?? "");
            return data;
        }

        private static RoomDefinitionData ParseRoomDefinitionData(string wiredData)
        {
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return null;
            var data = JsonConvert.DeserializeObject<RoomDefinitionData>(wiredData);
            if (data == null) return null;
            data.variableName = WiredVariableNameValidator.NormalizeLegacy(data.variableName ?? "");
            return data;
        }

        // ── Key helpers ──────────────────────────────────────────────────────
        private static string CreateDefinitionPrefix(int roomId, int itemId) => $"{roomId}:{itemId}";
        private static string CreateUserCacheKey(int roomId, int itemId, int userId) => $"{roomId}:{itemId}:{userId}";
        private static string CreateRoomCacheKey(int roomId, int itemId) => $"{roomId}:{itemId}";

        private static int NormalizeTimestamp(int value, int fallback)
        {
            if (value > 0) return value;
            if (fallback > 0) return fallback;
            return PolarEnvironment.GetIUnixTimestamp();
        }

        // ── Public DTOs ──────────────────────────────────────────────────────
        public class RoomOption
        {
            public int RoomId { get; }
            public string RoomName { get; }
            public List<SharedDefinitionOption> Variables { get; }

            public RoomOption(int roomId, string roomName, List<SharedDefinitionOption> variables)
            {
                RoomId = roomId;
                RoomName = roomName;
                Variables = variables;
            }
        }

        public class SharedDefinitionOption
        {
            public int RoomId { get; }
            public string RoomName { get; }
            public int ItemId { get; }
            public string Name { get; }
            public int TargetType { get; }
            public bool HasValue { get; }

            public SharedDefinitionOption(int roomId, string roomName, int itemId, string name, int targetType, bool hasValue)
            {
                RoomId = roomId;
                RoomName = roomName;
                ItemId = itemId;
                Name = name;
                TargetType = targetType;
                HasValue = hasValue;
            }
        }

        public class SharedUserAssignment
        {
            public int? Value { get; }
            public int CreatedAt { get; }
            public int UpdatedAt { get; }

            public SharedUserAssignment(int? value, int createdAt, int updatedAt)
            {
                Value = value;
                CreatedAt = createdAt;
                UpdatedAt = updatedAt;
            }
        }

        public class SharedRoomAssignment
        {
            public int Value { get; }
            public int UpdatedAt { get; }

            public SharedRoomAssignment(int value, int updatedAt)
            {
                Value = value;
                UpdatedAt = updatedAt;
            }
        }

        // ── Private cache wrappers ───────────────────────────────────────────
        private class CachedUserAssignment
        {
            public bool Present { get; }
            public SharedUserAssignment Assignment { get; }

            private CachedUserAssignment(bool present, SharedUserAssignment a) { Present = present; Assignment = a; }

            public static CachedUserAssignment FromAssignment(SharedUserAssignment a) => new(true, a);
            public static CachedUserAssignment Missing() => new(false, null);
        }

        private class CachedRoomAssignment
        {
            public bool Present { get; }
            public SharedRoomAssignment Assignment { get; }

            private CachedRoomAssignment(bool present, SharedRoomAssignment a) { Present = present; Assignment = a; }

            public static CachedRoomAssignment FromAssignment(SharedRoomAssignment a) => new(true, a);
            public static CachedRoomAssignment Missing() => new(false, null);
        }

        // ── Internal JSON DTOs ───────────────────────────────────────────────
        private class UserDefinitionData
        {
            public string variableName { get; set; }
            public bool hasValue { get; set; }
            public int availability { get; set; }
        }

        private class RoomDefinitionData
        {
            public string variableName { get; set; }
            public int availability { get; set; }
        }
    }
}