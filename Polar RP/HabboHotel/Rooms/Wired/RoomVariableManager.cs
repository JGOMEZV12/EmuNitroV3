using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using System.Linq;

namespace Polar.HabboHotel.Rooms.Wired
{
    /// <summary>
    /// Manages wired variable assignments scoped to the room itself (not a user/furni).
    /// Mirrors Java RoomVariableManager.
    /// Persistent values are stored in room_wired_variables.
    /// </summary>
    public sealed class RoomVariableManager
    {
        // ── Fields ────────────────────────────────────────────────────────────────

        private readonly Room _room;

        // definitionItemId → assignment
        private readonly ConcurrentDictionary<int, VariableAssignment> _assignmentsByDefId = new();

        private volatile bool _persistentLoaded;
        private readonly object _loadLock = new();

        // ── Construction ──────────────────────────────────────────────────────────

        public RoomVariableManager(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>Lazy-loads persistent values from the DB (double-check lock).</summary>
        public void EnsurePersistentValuesLoaded()
        {
            if (_persistentLoaded) return;
            lock (_loadLock)
            {
                if (_persistentLoaded) return;

                var stale = new List<int>();

                try
                {
                    using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery(
                        "SELECT variable_item_id, value, created_at, updated_at " +
                        "FROM room_wired_variables WHERE room_id = @rid");
                    db.AddParameter("rid", _room.Id);
                    var table = db.getTable();

                    if (table != null)
                    {
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            int defId = Convert.ToInt32(row["variable_item_id"]);
                            int value = Convert.ToInt32(row["value"]);
                            int updatedAt = NormalizeTs(Convert.ToInt32(row["updated_at"]), 0);

                            _assignmentsByDefId[defId] = new VariableAssignment(value, 0, updatedAt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogCriticalException(
                        $"[RoomVariableManager] Load room={_room.Id}: {ex}");
                }

                foreach (int defId in stale)
                    DeletePersistentAssignment(defId);

                _persistentLoaded = true;
            }
        }

        public int GetCurrentValue(int definitionItemId)
        {
            EnsurePersistentValuesLoaded();
            if (definitionItemId <= 0) return 0;

            return _assignmentsByDefId.TryGetValue(definitionItemId, out var a)
                ? a.Value
                : 0;
        }

        public bool HasVariable(int definitionItemId)
        {
            if (definitionItemId <= 0) return false;
            EnsurePersistentValuesLoaded();
            return _assignmentsByDefId.ContainsKey(definitionItemId);
        }

        /// <summary>
        /// Updates (or creates) a room variable value.
        /// Mirrors Java updateVariableValue(int definitionItemId, int value).
        /// </summary>
        public bool UpdateVariableValue(int definitionItemId, int value)
        {
            EnsurePersistentValuesLoaded();
            if (definitionItemId <= 0) return false;

            int? previousValue = _assignmentsByDefId.TryGetValue(definitionItemId, out var existing)
                ? existing.Value
                : null;

            int now = PolarEnvironment.GetUnixTimestamp();

            if (existing == null)
            {
                existing = new VariableAssignment(value, 0, now);
                _assignmentsByDefId[definitionItemId] = existing;
            }
            else if (existing.Value == value)
            {
                // Unchanged — still emit the event (mirrors Java behaviour)
                EmitVariableChangedEvent(definitionItemId, previousValue, value);
                return false;
            }
            else
            {
                existing.SetValue(value, now);
            }

            UpsertPersistentAssignment(definitionItemId, existing);
            EmitVariableChangedEvent(definitionItemId, previousValue, value);
            BroadcastSnapshot();
            return true;
        }

        public bool RemoveVariable(int definitionItemId)
        {
            EnsurePersistentValuesLoaded();
            if (definitionItemId <= 0) return false;

            if (!_assignmentsByDefId.TryRemove(definitionItemId, out var removed))
                return false;

            DeletePersistentAssignment(definitionItemId);
            EmitVariableChangedEvent(definitionItemId, removed.Value, null);
            BroadcastSnapshot();
            return true;
        }

        /// <summary>
        /// Clears all transient (non-persistent) in-memory assignments.
        /// Called when a room is unloaded. Mirrors Java clearTransientAssignments().
        /// </summary>
        public void ClearTransientAssignments()
        {
            EnsurePersistentValuesLoaded();
            // In this simplified port all assignments without a persisted row are
            // considered transient. We remove everything that is NOT in the DB
            // (i.e. was assigned at runtime without explicit persist).
            // The simplest safe behaviour: clear everything from memory;
            // the DB still holds any permanent rows and will reload on next access.
            bool changed = !_assignmentsByDefId.IsEmpty;
            _assignmentsByDefId.Clear();
            _persistentLoaded = false; // Force reload on next access
            if (changed) BroadcastSnapshot();
        }

        // ── Snapshot ──────────────────────────────────────────────────────────────

        public Snapshot CreateSnapshot()
        {
            EnsurePersistentValuesLoaded();

            var assignments = _assignmentsByDefId
                .OrderBy(kv => kv.Key)
                .Select(kv => new AssignmentEntry(
                    kv.Key,
                    kv.Value.Value,
                    kv.Value.CreatedAt,
                    kv.Value.UpdatedAt))
                .ToList();

            return new Snapshot(_room.Id, assignments);
        }

        public void SendSnapshot(Habbo habbo)
        {
            if (habbo?.GetClient() == null || !_room.canInspectWired(habbo)) return;
           /* habbo.GetClient().SendMessage(new WiredUserVariablesDataComposer(
                _room.GetUserVariableManager().CreateSnapshot(),
                _room.GetFurniVariableManager().CreateSnapshot(),
                CreateSnapshot()));*/
        }

        public void BroadcastSnapshot()
        {
            var userSnap = _room.GetUserVariableManager().CreateSnapshot();
            var furniSnap = _room.GetFurniVariableManager().CreateSnapshot();
            var roomSnap = CreateSnapshot();

            foreach (var roomUser in _room.GetRoomUserManager().GetUserList())
            {
                var habbo = roomUser?.GetClient()?.GetHabbo();
                if (habbo == null || !_room.canInspectWired(habbo)) continue;
                roomUser.GetClient().SendMessage(
                    new WiredUserVariablesDataComposer(userSnap, furniSnap, roomSnap));
            }
        }

        // ── DB helpers ────────────────────────────────────────────────────────────

        private void UpsertPersistentAssignment(int defId, VariableAssignment a)
        {
            try
            {
                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(
                    "INSERT INTO room_wired_variables " +
                    "(room_id, variable_item_id, value, created_at, updated_at) " +
                    "VALUES (@rid, @did, @val, 0, @ua) " +
                    "ON DUPLICATE KEY UPDATE value=VALUES(value), updated_at=VALUES(updated_at)");
                db.AddParameter("rid", _room.Id);
                db.AddParameter("did", defId);
                db.AddParameter("val", a?.Value ?? 0);
                db.AddParameter("ua", a?.UpdatedAt ?? PolarEnvironment.GetUnixTimestamp());
                db.RunQuery();
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException(
                    $"[RoomVariableManager] Upsert room={_room.Id} def={defId}: {ex}");
            }
        }

        private void DeletePersistentAssignment(int defId)
        {
            try
            {
                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(
                    "DELETE FROM room_wired_variables " +
                    "WHERE room_id=@rid AND variable_item_id=@did");
                db.AddParameter("rid", _room.Id);
                db.AddParameter("did", defId);
                db.RunQuery();
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException(
                    $"[RoomVariableManager] Delete room={_room.Id} def={defId}: {ex}");
            }
        }

        // ── Event emission ────────────────────────────────────────────────────────

        private void EmitVariableChangedEvent(int defId, int? previousValue, int? currentValue)
        {
            var changeKind = ResolveChangeKind(previousValue, currentValue);
            if (changeKind == VariableChangeKind.None) return;

            WiredComponent.TriggerRoomVariableChanged(_room, defId, changeKind);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static int NormalizeTs(int v, int fallback)
        {
            if (v > 0) return v;
            if (fallback > 0) return fallback;
            return 0;
        }

        private static VariableChangeKind ResolveChangeKind(int? prev, int? cur)
        {
            if (prev == cur) return VariableChangeKind.Unchanged;
            if (prev == null || cur == null) return VariableChangeKind.None;
            return cur > prev ? VariableChangeKind.Increased : VariableChangeKind.Decreased;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  INNER TYPES
        // ══════════════════════════════════════════════════════════════════════════

        private sealed class VariableAssignment
        {
            public int Value { get; private set; }
            public int CreatedAt { get; }
            public int UpdatedAt { get; private set; }

            public VariableAssignment(int value, int createdAt, int updatedAt)
            {
                Value = value;
                CreatedAt = createdAt;
                UpdatedAt = updatedAt;
            }

            public void SetValue(int value, int updatedAt)
            {
                Value = value;
                UpdatedAt = updatedAt;
            }
        }

        // ── Snapshot DTOs ─────────────────────────────────────────────────────────

        public sealed class Snapshot
        {
            public int RoomId { get; }
            public IReadOnlyList<AssignmentEntry> Assignments { get; }

            public Snapshot(int roomId, List<AssignmentEntry> assignments)
            {
                RoomId = roomId;
                Assignments = assignments.AsReadOnly();
            }
        }

        public sealed class AssignmentEntry
        {
            public int VariableItemId { get; }
            public int Value { get; }
            public int CreatedAt { get; }
            public int UpdatedAt { get; }

            public AssignmentEntry(int variableItemId, int value, int createdAt, int updatedAt)
            {
                VariableItemId = variableItemId;
                Value = value;
                CreatedAt = createdAt;
                UpdatedAt = updatedAt;
            }
        }
    }
}