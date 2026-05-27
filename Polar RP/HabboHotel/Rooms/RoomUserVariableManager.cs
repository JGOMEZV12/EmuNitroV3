using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Rooms
{
    public enum VariableChangeKind { None, Unchanged, Increased, Decreased }
    public class RoomUserVariableManager
    {
        private readonly Room _room;
        // userId → (definitionItemId → VariableAssignment)
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, VariableAssignment>> _activeAssignments = new();

        public RoomUserVariableManager(Room room) => _room = room;

        // ── AssignVariable ───────────────────────────────────────────────────
        public bool AssignVariable(Habbo habbo, int definitionItemId, int? value, bool overrideExisting)
        {
            if (habbo == null || definitionItemId <= 0) return false;

            var def = GetDefinition(definitionItemId);
            if (def == null) return false;

            int userId = habbo.Id;
            int? normalizedValue = def.HasValue ? value : null;

            var assignments = _activeAssignments.GetOrAdd(userId, _ => new ConcurrentDictionary<int, VariableAssignment>());
            var existing = assignments.GetValueOrDefault(definitionItemId);

            if (existing != null && !overrideExisting) return false;

            bool valueChanged = existing == null || existing.Value != normalizedValue;
            if (!valueChanged && existing != null) return false;

            int now = PolarEnvironment.GetIUnixTimestamp();
            assignments[definitionItemId] = new VariableAssignment(normalizedValue, now, now);

            if (def.IsPermanent)
                UpsertPersistentAssignment(userId, definitionItemId, assignments[definitionItemId]);

            BroadcastSnapshot();
            return true;
        }

        // ── UpdateVariableValue ──────────────────────────────────────────────
        public bool UpdateVariableValue(int userId, int definitionItemId, int? value)
        {
            if (userId <= 0 || definitionItemId <= 0) return false;

            var def = GetDefinition(definitionItemId);
            if (def == null || !def.HasValue) return false;

            if (!_activeAssignments.TryGetValue(userId, out var assignments)) return false;
            if (!assignments.TryGetValue(definitionItemId, out var assignment)) return false;
            if (assignment.Value == value) return false;

            assignment.SetValue(value, PolarEnvironment.GetIUnixTimestamp());

            if (def.IsPermanent)
                UpsertPersistentAssignment(userId, definitionItemId, assignment);

            BroadcastSnapshot();
            return true;
        }

        // ── GetCurrentValue ──────────────────────────────────────────────────
        public int GetCurrentValue(int userId, int definitionItemId)
        {
            if (!_activeAssignments.TryGetValue(userId, out var assignments)) return 0;
            if (!assignments.TryGetValue(definitionItemId, out var assignment)) return 0;
            return assignment.Value ?? 0;
        }

        public int GetCreatedAt(int userId, int definitionItemId)
        {
            if (!_activeAssignments.TryGetValue(userId, out var a)) return 0;
            return a.TryGetValue(definitionItemId, out var v) ? v.CreatedAt : 0;
        }

        public int GetUpdatedAt(int userId, int definitionItemId)
        {
            if (!_activeAssignments.TryGetValue(userId, out var a)) return 0;
            return a.TryGetValue(definitionItemId, out var v) ? v.UpdatedAt : 0;
        }

        public bool HasVariable(int userId, int definitionItemId)
        {
            if (!_activeAssignments.TryGetValue(userId, out var a)) return false;
            return a.ContainsKey(definitionItemId);
        }

        public bool RemoveVariable(int userId, int definitionItemId)
        {
            if (!_activeAssignments.TryGetValue(userId, out var assignments)) return false;
            if (!assignments.TryRemove(definitionItemId, out _)) return false;

            if (assignments.IsEmpty) _activeAssignments.TryRemove(userId, out _);

            DeletePersistentAssignment(userId, definitionItemId);
            BroadcastSnapshot();
            return true;
        }

        public void ClearAssignmentsForUser(int userId)
        {
            if (_activeAssignments.TryRemove(userId, out _))
                BroadcastSnapshot();
        }

        // ── Restore permanent assignments when user enters room ───────────────
        public void RestorePermanentAssignments(Habbo habbo)
        {
            if (habbo == null) return;

            int userId = habbo.Id;
            var restored = new ConcurrentDictionary<int, VariableAssignment>();

            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("SELECT variable_item_id, value, created_at, updated_at FROM room_user_wired_variables WHERE room_id = @roomId AND user_id = @userId");
                dbClient.AddParameter("roomId", _room.Id);
                dbClient.AddParameter("userId", userId);

                var table = dbClient.getTable();
                if (table == null) return;

                foreach (System.Data.DataRow row in table.Rows)
                {
                    int definitionItemId = Convert.ToInt32(row["variable_item_id"]);
                    var def = GetDefinition(definitionItemId);
                    if (def == null || !def.IsPermanent)
                    {
                        DeletePersistentAssignment(userId, definitionItemId);
                        continue;
                    }

                    int? value = row.IsNull("value") ? (int?)null : Convert.ToInt32(row["value"]);
                    int createdAt = NormalizeTimestamp(Convert.ToInt32(row["created_at"]), 0);
                    int updatedAt = NormalizeTimestamp(Convert.ToInt32(row["updated_at"]), createdAt);
                    restored[definitionItemId] = new VariableAssignment(value, createdAt, updatedAt);
                }
            }
            catch (Exception ex)
            {
                Logging.LogException($"[RoomUserVariableManager] RestorePermanentAssignments: {ex}");
            }

            if (restored.IsEmpty) _activeAssignments.TryRemove(userId, out _);
            else _activeAssignments[userId] = restored;

            BroadcastSnapshot();
        }

        // ── Snapshot ─────────────────────────────────────────────────────────
        public Snapshot CreateSnapshot()
        {
            var definitions = GetAllDefinitions()
                .Select(d => new DefinitionEntry(d.Id, d.VariableName, d.HasValue, d.Availability, false, false))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var users = new List<UserAssignmentsEntry>();

            foreach (var kvp in _activeAssignments)
            {
                int userId = kvp.Key;
                var assignments = kvp.Value
                    .Select(a => new AssignmentEntry(a.Key, a.Value.Value, a.Value.CreatedAt, a.Value.UpdatedAt))
                    .OrderBy(a => a.VariableItemId)
                    .ToList();

                if (assignments.Count > 0)
                    users.Add(new UserAssignmentsEntry(userId, assignments));
            }

            users.Sort((a, b) => a.UserId.CompareTo(b.UserId));
            return new Snapshot(_room.Id, definitions, users);
        }

        public void BroadcastSnapshot()
        {
            // TODO: enviar WiredUserVariablesDataComposer a usuarios con permisos
            // Implementar cuando el composer esté listo
        }

        // ── DB helpers ────────────────────────────────────────────────────────
        private void UpsertPersistentAssignment(int userId, int definitionItemId, VariableAssignment assignment)
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("INSERT INTO room_user_wired_variables (room_id, user_id, variable_item_id, value, created_at, updated_at) VALUES (@r, @u, @i, @v, @ca, @ua) ON DUPLICATE KEY UPDATE value = VALUES(value), updated_at = VALUES(updated_at)");
                dbClient.AddParameter("r", _room.Id);
                dbClient.AddParameter("u", userId);
                dbClient.AddParameter("i", definitionItemId);
                dbClient.AddParameter("v", assignment?.Value.HasValue == true ? (object)assignment.Value.Value : DBNull.Value);
                dbClient.AddParameter("ca", assignment?.CreatedAt ?? PolarEnvironment.GetIUnixTimestamp());
                dbClient.AddParameter("ua", assignment?.UpdatedAt ?? PolarEnvironment.GetIUnixTimestamp());
                dbClient.RunQuery();
            }
            catch (Exception ex) { Logging.LogException($"[RoomUserVariableManager] UpsertPersistent: {ex}"); }
        }

        private void DeletePersistentAssignment(int userId, int definitionItemId)
        {
            try
            {
                using IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                dbClient.SetQuery("DELETE FROM room_user_wired_variables WHERE room_id = @r AND user_id = @u AND variable_item_id = @i");
                dbClient.AddParameter("r", _room.Id);
                dbClient.AddParameter("u", userId);
                dbClient.AddParameter("i", definitionItemId);
                dbClient.RunQuery();
            }
            catch (Exception ex) { Logging.LogException($"[RoomUserVariableManager] DeletePersistent: {ex}"); }
        }

        // ── Definition helpers ────────────────────────────────────────────────
        private VariableDefinition GetDefinition(int definitionItemId)
        {
            if (_room.GetWired() == null || !_room.GetWired().TryGet(definitionItemId, out var box)) return null;
            if (box is not AddonSetVariableBox varBox) return null;
            if (string.IsNullOrEmpty(varBox.VariableName)) return null;

            return new VariableDefinition(
                varBox.Item.Id,
                varBox.VariableName,
                varBox.HasValue,
                varBox.Availability,
                varBox.IsPermanent);
        }

        private List<VariableDefinition> GetAllDefinitions()
        {
            var result = new List<VariableDefinition>();
            if (_room.GetWired() == null) return result;

            foreach (var item in _room.GetRoomItemHandler().GetFloor)
            {
                if (item == null) continue;
                var def = GetDefinition(item.Id);
                if (def != null) result.Add(def);
            }

            return result.OrderBy(d => d.VariableName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int NormalizeTimestamp(int value, int fallback)
        {
            if (value > 0) return value;
            if (fallback > 0) return fallback;
            return PolarEnvironment.GetIUnixTimestamp();
        }

        // ── Internal DTOs ─────────────────────────────────────────────────────
        private class VariableDefinition
        {
            public int Id { get; }
            public string VariableName { get; }
            public bool HasValue { get; }
            public int Availability { get; }
            public bool IsPermanent { get; }

            public VariableDefinition(int id, string name, bool hasValue, int availability, bool isPermanent)
            {
                Id = id;
                VariableName = name;
                HasValue = hasValue;
                Availability = availability;
                IsPermanent = isPermanent;
            }
        }

        private class VariableAssignment
        {
            public int? Value { get; private set; }
            public int CreatedAt { get; }
            public int UpdatedAt { get; private set; }

            public VariableAssignment(int? value, int createdAt, int updatedAt)
            {
                Value = value;
                CreatedAt = createdAt;
                UpdatedAt = updatedAt;
            }

            public void SetValue(int? value, int updatedAt) { Value = value; UpdatedAt = updatedAt; }
        }

        // ── Public Snapshot DTOs ──────────────────────────────────────────────
        public class Snapshot
        {
            public int RoomId { get; }
            public List<DefinitionEntry> Definitions { get; }
            public List<UserAssignmentsEntry> Users { get; }

            public Snapshot(int roomId, List<DefinitionEntry> definitions, List<UserAssignmentsEntry> users)
            {
                RoomId = roomId;
                Definitions = definitions;
                Users = users;
            }
        }

        public class DefinitionEntry
        {
            public int ItemId { get; }
            public string Name { get; }
            public bool HasValue { get; }
            public int Availability { get; }
            public bool TextConnected { get; }
            public bool ReadOnly { get; }

            public DefinitionEntry(int itemId, string name, bool hasValue, int availability, bool textConnected, bool readOnly)
            {
                ItemId = itemId;
                Name = name;
                HasValue = hasValue;
                Availability = availability;
                TextConnected = textConnected;
                ReadOnly = readOnly;
            }
        }

        public class UserAssignmentsEntry
        {
            public int UserId { get; }
            public List<AssignmentEntry> Assignments { get; }

            public UserAssignmentsEntry(int userId, List<AssignmentEntry> assignments)
            {
                UserId = userId;
                Assignments = assignments;
            }
        }

        public class AssignmentEntry
        {
            public int VariableItemId { get; }
            public int? Value { get; }
            public int CreatedAt { get; }
            public int UpdatedAt { get; }

            public AssignmentEntry(int variableItemId, int? value, int createdAt, int updatedAt)
            {
                VariableItemId = variableItemId;
                Value = value;
                CreatedAt = createdAt;
                UpdatedAt = updatedAt;
            }

            public bool HasValue => Value.HasValue;
        }
    }
}