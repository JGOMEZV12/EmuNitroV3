using MySqlConnector;
using Polar.Core;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Rooms.Wired
{
    /// <summary>
    /// Manages wired variable assignments scoped to furniture items in a room.
    /// Mirrors Java RoomFurniVariableManager — permanent assignments are persisted
    /// in room_furni_wired_variables; transient ones live only in memory.
    /// </summary>
    public sealed class RoomFurniVariableManager
    {
        // ── Fields ────────────────────────────────────────────────────────────────

        private readonly Room _room;

        // furniId → (definitionItemId → assignment)
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, VariableAssignment>>
            _assignmentsByFurniId = new();

        private volatile bool _permanentLoaded;
        private readonly object _loadLock = new();

        // ── Construction ──────────────────────────────────────────────────────────

        public RoomFurniVariableManager(Room room)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>Lazy-loads permanent assignments from DB (double-check lock).</summary>
        public void EnsurePermanentAssignmentsLoaded()
        {
            if (_permanentLoaded) return;
            lock (_loadLock)
            {
                if (_permanentLoaded) return;

                var stale = new List<(int furniId, int defId)>();

                try
                {
                    using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                    db.SetQuery(
                        "SELECT furni_id, variable_item_id, value, created_at, updated_at " +
                        "FROM room_furni_wired_variables WHERE room_id = @rid");
                    db.AddParameter("rid", _room.Id);
                    var table = db.getTable();

                    if (table != null)
                    {
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            int furniId = Convert.ToInt32(row["furni_id"]);
                            int defId = Convert.ToInt32(row["variable_item_id"]);
                            Item furni = _room.GetRoomItemHandler().GetItem(furniId);

                            if (furni == null)
                            {
                                stale.Add((furniId, defId));
                                continue;
                            }

                            int? value = row["value"] == DBNull.Value
                                ? null
                                : (int?)Convert.ToInt32(row["value"]);

                            int createdAt = NormalizeTs(Convert.ToInt32(row["created_at"]), 0);
                            int updatedAt = NormalizeTs(Convert.ToInt32(row["updated_at"]), createdAt);

                            _assignmentsByFurniId
                                .GetOrAdd(furniId, _ => new ConcurrentDictionary<int, VariableAssignment>())
                                [defId] = new VariableAssignment(value, createdAt, updatedAt);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogCriticalException(
                        $"[RoomFurniVariableManager] Failed to load room {_room.Id}: {ex}");
                }

                foreach (var (furniId, defId) in stale)
                    DeletePersistentAssignment(furniId, defId);

                _permanentLoaded = true;
            }
        }

        public bool AssignVariable(Item furni, int definitionItemId, int? value, bool overrideExisting)
        {
            if (furni == null || definitionItemId <= 0) return false;

            EnsurePermanentAssignmentsLoaded();

            var assignments = _assignmentsByFurniId
                .GetOrAdd(furni.Id, _ => new ConcurrentDictionary<int, VariableAssignment>());

            if (assignments.TryGetValue(definitionItemId, out var existing) && !overrideExisting)
                return false;

            bool hadBefore = existing != null;
            int? prevValue = existing?.Value;
            int now = PolarEnvironment.GetUnixTimestamp();
            bool valueChanged = existing == null || existing.Value != value;

            if (existing == null || overrideExisting)
                assignments[definitionItemId] = new VariableAssignment(value, now, now);
            else if (valueChanged)
                existing.SetValue(value, now);

            // Persist / remove depending on permanence flag
            // (we don't have WiredExtraFurniVariable here — callers control permanence via overload)
            UpsertPersistentAssignment(furni.Id, definitionItemId, assignments[definitionItemId]);

            if (valueChanged || (hadBefore && overrideExisting && prevValue == value))
            {
                bool hasAfter = HasVariable(furni.Id, definitionItemId);
                int? afterValue = hasAfter ? GetCurrentValue(furni.Id, definitionItemId) : null;
                EmitVariableChangedEvent(furni.Id, definitionItemId, existedBefore: hadBefore,
                    previousValue: prevValue, existsAfter: hasAfter, currentValue: afterValue);
            }

            if (valueChanged) BroadcastSnapshot();
            return valueChanged;
        }

        public bool UpdateVariableValue(int furniId, int definitionItemId, int? value)
        {
            EnsurePermanentAssignmentsLoaded();

            if (!_assignmentsByFurniId.TryGetValue(furniId, out var assignments)) return false;
            if (!assignments.TryGetValue(definitionItemId, out var assignment)) return false;

            int? prev = assignment.Value;
            if (prev == value)
            {
                EmitVariableChangedEvent(furniId, definitionItemId,
                    existedBefore: true, previousValue: prev,
                    existsAfter: true, currentValue: assignment.Value);
                return false;
            }

            assignment.SetValue(value, PolarEnvironment.GetUnixTimestamp());
            UpsertPersistentAssignment(furniId, definitionItemId, assignment);
            EmitVariableChangedEvent(furniId, definitionItemId,
                existedBefore: true, previousValue: prev,
                existsAfter: true, currentValue: assignment.Value);
            BroadcastSnapshot();
            return true;
        }

        public bool RemoveVariable(int furniId, int definitionItemId)
        {
            EnsurePermanentAssignmentsLoaded();
            if (furniId <= 0 || definitionItemId <= 0) return false;

            if (!_assignmentsByFurniId.TryGetValue(furniId, out var assignments)) return false;

            bool hadBefore = assignments.TryGetValue(definitionItemId, out var prev);
            if (!hadBefore) return false;

            assignments.TryRemove(definitionItemId, out _);
            if (assignments.IsEmpty) _assignmentsByFurniId.TryRemove(furniId, out _);

            DeletePersistentAssignment(furniId, definitionItemId);
            EmitVariableChangedEvent(furniId, definitionItemId,
                existedBefore: true, previousValue: prev?.Value,
                existsAfter: false, currentValue: null);
            BroadcastSnapshot();
            return true;
        }

        public int GetCurrentValue(int furniId, int definitionItemId)
        {
            EnsurePermanentAssignmentsLoaded();
            if (furniId <= 0 || definitionItemId <= 0) return 0;

            if (!_assignmentsByFurniId.TryGetValue(furniId, out var assignments)) return 0;
            if (!assignments.TryGetValue(definitionItemId, out var assignment)) return 0;
            return assignment.Value ?? 0;
        }

        public bool HasVariable(int furniId, int definitionItemId)
        {
            EnsurePermanentAssignmentsLoaded();
            if (furniId <= 0 || definitionItemId <= 0) return false;

            return _assignmentsByFurniId.TryGetValue(furniId, out var a) &&
                   a.ContainsKey(definitionItemId);
        }

        public void RemoveAssignmentsForFurni(int furniId)
        {
            EnsurePermanentAssignmentsLoaded();
            if (furniId <= 0) return;

            bool changed = _assignmentsByFurniId.TryRemove(furniId, out _);
            DeletePersistentAssignmentsForFurni(furniId);
            if (changed) BroadcastSnapshot();
        }

        public void ClearTransientAssignments()
        {
            EnsurePermanentAssignmentsLoaded();
            // Without WiredExtraFurniVariable, we treat ALL in-memory as transient
            // (callers that need permanent storage go through UpsertPersistentAssignment)
            bool changed = !_assignmentsByFurniId.IsEmpty;
            _assignmentsByFurniId.Clear();
            if (changed) BroadcastSnapshot();
        }

        // ── Snapshot ──────────────────────────────────────────────────────────────

        public Snapshot CreateSnapshot()
        {
            EnsurePermanentAssignmentsLoaded();

            var furnis = new List<FurniAssignmentsEntry>();

            foreach (var (furniId, assignments) in _assignmentsByFurniId)
            {
                if (_room.GetRoomItemHandler().GetItem(furniId) == null) continue;

                var entries = assignments
                    .OrderBy(kv => kv.Key)
                    .Select(kv => new AssignmentEntry(
                        kv.Key,
                        kv.Value.Value,
                        kv.Value.CreatedAt,
                        kv.Value.UpdatedAt))
                    .ToList();

                if (entries.Count > 0)
                    furnis.Add(new FurniAssignmentsEntry(furniId, entries));
            }

            furnis.Sort((a, b) => a.FurniId.CompareTo(b.FurniId));
            return new Snapshot(_room.Id, furnis);
        }

        public void SendSnapshot(Habbo habbo)
        {
            if (habbo?.GetClient() == null || !_room.canInspectWired(habbo)) return;
            habbo.GetClient().SendMessage(new WiredUserVariablesDataComposer(
                _room.GetUserVariableManager().CreateSnapshot(),
                CreateSnapshot(),
                _room.GetRoomVariableManager().CreateSnapshot()));
        }

        public void BroadcastSnapshot()
        {
            var userSnap = _room.GetUserVariableManager().CreateSnapshot();
            var furniSnap = CreateSnapshot();
            var roomSnap = _room.GetRoomVariableManager().CreateSnapshot();

            foreach (var roomUser in _room.GetRoomUserManager().GetUserList())
            {
                var habbo = roomUser?.GetClient()?.GetHabbo();
                if (habbo == null || !_room.canInspectWired(habbo)) continue;
                roomUser.GetClient().SendMessage(
                    new WiredUserVariablesDataComposer(userSnap, furniSnap, roomSnap));
            }
        }

        // ── DB helpers ────────────────────────────────────────────────────────────

        private void UpsertPersistentAssignment(int furniId, int defId, VariableAssignment a)
        {
            try
            {
                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(
                    "INSERT INTO room_furni_wired_variables " +
                    "(room_id, furni_id, variable_item_id, value, created_at, updated_at) " +
                    "VALUES (@rid, @fid, @did, @val, @ca, @ua) " +
                    "ON DUPLICATE KEY UPDATE value=VALUES(value), updated_at=VALUES(updated_at)");
                db.AddParameter("rid", _room.Id);
                db.AddParameter("fid", furniId);
                db.AddParameter("did", defId);
                db.AddParameter("val", (object?)a?.Value ?? DBNull.Value);
                int now = PolarEnvironment.GetUnixTimestamp();
                db.AddParameter("ca", a?.CreatedAt ?? now);
                db.AddParameter("ua", a?.UpdatedAt ?? now);
                db.RunQuery();
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException(
                    $"[RoomFurniVariableManager] Upsert room={_room.Id} furni={furniId} def={defId}: {ex}");
            }
        }

        private void DeletePersistentAssignment(int furniId, int defId)
        {
            try
            {
                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(
                    "DELETE FROM room_furni_wired_variables " +
                    "WHERE room_id=@rid AND furni_id=@fid AND variable_item_id=@did");
                db.AddParameter("rid", _room.Id);
                db.AddParameter("fid", furniId);
                db.AddParameter("did", defId);
                db.RunQuery();
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException(
                    $"[RoomFurniVariableManager] Delete room={_room.Id} furni={furniId} def={defId}: {ex}");
            }
        }

        private void DeletePersistentAssignmentsForFurni(int furniId)
        {
            try
            {
                using IQueryAdapter db = PolarEnvironment.GetDatabaseManager().GetQueryReactor();
                db.SetQuery(
                    "DELETE FROM room_furni_wired_variables WHERE room_id=@rid AND furni_id=@fid");
                db.AddParameter("rid", _room.Id);
                db.AddParameter("fid", furniId);
                db.RunQuery();
            }
            catch (Exception ex)
            {
                Logging.LogCriticalException(
                    $"[RoomFurniVariableManager] DeleteForFurni room={_room.Id} furni={furniId}: {ex}");
            }
        }

        // ── Event emission ────────────────────────────────────────────────────────

        private void EmitVariableChangedEvent(
            int furniId, int defId,
            bool existedBefore, int? previousValue,
            bool existsAfter, int? currentValue)
        {
            bool created = !existedBefore && existsAfter;
            bool deleted = existedBefore && !existsAfter;
            var changeKind = ResolveChangeKind(existedBefore, previousValue, existsAfter, currentValue);

            if (!created && !deleted && changeKind == VariableChangeKind.None) return;

            WiredComponent.TriggerFurniVariableChanged(
                _room, furniId, defId, created, deleted, changeKind);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static int NormalizeTs(int v, int fallback)
        {
            if (v > 0) return v;
            if (fallback > 0) return fallback;
            return PolarEnvironment.GetUnixTimestamp();
        }

        private static VariableChangeKind ResolveChangeKind(
            bool existedBefore, int? prev,
            bool existsAfter, int? cur)
        {
            if (!existedBefore || !existsAfter) return VariableChangeKind.None;
            if (prev == cur) return VariableChangeKind.Unchanged;
            int p = prev ?? 0, c = cur ?? 0;
            return c > p ? VariableChangeKind.Increased : VariableChangeKind.Decreased;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  INNER TYPES
        // ══════════════════════════════════════════════════════════════════════════

        private sealed class VariableAssignment
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

            public void SetValue(int? value, int updatedAt)
            {
                Value = value;
                UpdatedAt = updatedAt;
            }
        }

        // ── Snapshot DTOs ─────────────────────────────────────────────────────────

        public sealed class Snapshot
        {
            public int RoomId { get; }
            public IReadOnlyList<FurniAssignmentsEntry> Furnis { get; }

            public Snapshot(int roomId, List<FurniAssignmentsEntry> furnis)
            {
                RoomId = roomId;
                Furnis = furnis.AsReadOnly();
            }
        }

        public sealed class FurniAssignmentsEntry
        {
            public int FurniId { get; }
            public IReadOnlyList<AssignmentEntry> Assignments { get; }

            public FurniAssignmentsEntry(int furniId, List<AssignmentEntry> assignments)
            {
                FurniId = furniId;
                Assignments = assignments.AsReadOnly();
            }
        }

        public sealed class AssignmentEntry
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