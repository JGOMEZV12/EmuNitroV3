using System;
using System.Collections.Generic;
using System.Linq;
using Polar.HabboHotel.Items.Wired.Boxes.Add_ons;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired
{
    public static class WiredVariableLevelSystemSupport
    {
        public const int TARGET_USER = 0;
        public const int TARGET_FURNI = 1;
        public const int TARGET_ROOM = 3;

        private const int SYNTHETIC_USER_OFFSET = 700_000_000;
        private const int SYNTHETIC_FURNI_OFFSET = 800_000_000;
        private const int SYNTHETIC_ROOM_OFFSET = 900_000_000;
        private const int SYNTHETIC_STRIDE = 16;

        // ── DerivedDefinition ────────────────────────────────────────────────
        public class DerivedDefinition
        {
            public int SyntheticItemId { get; }
            public int BaseDefinitionItemId { get; }
            public int SubvariableType { get; }
            public string VariableName { get; }
            public AddonVariableLevelUpSystemBox LevelSystem { get; }

            public DerivedDefinition(int syntheticItemId, int baseDefinitionItemId, int subvariableType,
                string variableName, AddonVariableLevelUpSystemBox levelSystem)
            {
                SyntheticItemId = syntheticItemId;
                BaseDefinitionItemId = baseDefinitionItemId;
                SubvariableType = subvariableType;
                VariableName = variableName;
                LevelSystem = levelSystem;
            }
        }

        public class LevelEntry
        {
            public int Level { get; }
            public int RequiredXp { get; }
            public LevelEntry(int level, int requiredXp) { Level = level; RequiredXp = requiredXp; }
        }

        // ── ResolveDerivedDefinition ─────────────────────────────────────────
        public static DerivedDefinition ResolveDerivedDefinition(Room room, int targetType, int syntheticItemId)
        {
            var decoded = DecodeSyntheticId(syntheticItemId);
            if (decoded == null || decoded.TargetType != targetType || room == null) return null;

            var wired = room.GetWired();
            if (wired == null || !wired.TryGet(decoded.BaseDefinitionItemId, out var baseBox)) return null;

            // Buscar el LevelUpSystem en la misma celda
            var levelSystem = GetLevelSystem(room, baseBox);
            if (levelSystem == null || !levelSystem.HasSubvariable(decoded.SubvariableType)) return null;

            string name = GetSubvariableKey(decoded.SubvariableType);

            return new DerivedDefinition(
                syntheticItemId,
                decoded.BaseDefinitionItemId,
                decoded.SubvariableType,
                name,
                levelSystem);
        }

        public static Integer GetDerivedValue(AddonVariableLevelUpSystemBox levelSystem, int subvariableType, int? baseValue)
        {
            if (levelSystem == null || !baseValue.HasValue) return null;

            var progress = CalculateProgress(levelSystem, baseValue.Value);

            return subvariableType switch
            {
                AddonVariableLevelUpSystemBox.SUB_CURRENT_LEVEL => progress.CurrentLevel,
                AddonVariableLevelUpSystemBox.SUB_CURRENT_XP => progress.CurrentXp,
                AddonVariableLevelUpSystemBox.SUB_LEVEL_PROGRESS => progress.ProgressXp,
                AddonVariableLevelUpSystemBox.SUB_LEVEL_PROGRESS_PCT => progress.ProgressPercent,
                AddonVariableLevelUpSystemBox.SUB_TOTAL_XP_REQUIRED => progress.TotalXpRequired,
                AddonVariableLevelUpSystemBox.SUB_XP_REMAINING => progress.XpRemaining,
                AddonVariableLevelUpSystemBox.SUB_IS_AT_MAX => progress.IsAtMax ? 1 : 0,
                AddonVariableLevelUpSystemBox.SUB_MAX_LEVEL => progress.MaxLevel,
                _ => (int?)null
            };
        }

        // ── LevelSystem lookup ───────────────────────────────────────────────
        private static AddonVariableLevelUpSystemBox GetLevelSystem(Room room, IWiredItem definition)
        {
            if (room == null || definition == null) return null;

            // Buscar en la misma celda
            var effects = room.GetWired().GetEffects(definition);
            foreach (var item in effects)
            {
                if (item is AddonVariableLevelUpSystemBox lvl) return lvl;
            }
            return null;
        }

        // ── Synthetic ID encode/decode ───────────────────────────────────────
        private static int CreateSyntheticItemId(int targetType, int baseDefinitionItemId, int subvariableType)
        {
            int offset = targetType switch
            {
                TARGET_FURNI => SYNTHETIC_FURNI_OFFSET,
                TARGET_ROOM => SYNTHETIC_ROOM_OFFSET,
                _ => SYNTHETIC_USER_OFFSET
            };
            return offset + (baseDefinitionItemId * SYNTHETIC_STRIDE) + (subvariableType + 1);
        }

        private static DecodedSyntheticId DecodeSyntheticId(int id)
        {
            if (id >= SYNTHETIC_ROOM_OFFSET) return DecodeSyntheticId(id, TARGET_ROOM, SYNTHETIC_ROOM_OFFSET);
            if (id >= SYNTHETIC_FURNI_OFFSET) return DecodeSyntheticId(id, TARGET_FURNI, SYNTHETIC_FURNI_OFFSET);
            if (id >= SYNTHETIC_USER_OFFSET) return DecodeSyntheticId(id, TARGET_USER, SYNTHETIC_USER_OFFSET);
            return null;
        }

        private static DecodedSyntheticId DecodeSyntheticId(int id, int targetType, int offset)
        {
            int local = id - offset;
            if (local < 0) return null;

            int encodedSub = local % SYNTHETIC_STRIDE;
            int baseItemId = local / SYNTHETIC_STRIDE;
            int subType = encodedSub - 1;

            if (baseItemId <= 0 || subType < 0 || subType >= AddonVariableLevelUpSystemBox.SUBVARIABLE_COUNT)
                return null;

            return new DecodedSyntheticId(targetType, baseItemId, subType);
        }

        private class DecodedSyntheticId
        {
            public int TargetType { get; }
            public int BaseDefinitionItemId { get; }
            public int SubvariableType { get; }

            public DecodedSyntheticId(int t, int b, int s) { TargetType = t; BaseDefinitionItemId = b; SubvariableType = s; }
        }

        // ── SubvariableKey ───────────────────────────────────────────────────
        private static string GetSubvariableKey(int subvariableType) => subvariableType switch
        {
            AddonVariableLevelUpSystemBox.SUB_CURRENT_LEVEL => "current_level",
            AddonVariableLevelUpSystemBox.SUB_CURRENT_XP => "current_xp",
            AddonVariableLevelUpSystemBox.SUB_LEVEL_PROGRESS => "level_progress",
            AddonVariableLevelUpSystemBox.SUB_LEVEL_PROGRESS_PCT => "level_progress_percent",
            AddonVariableLevelUpSystemBox.SUB_TOTAL_XP_REQUIRED => "total_xp_required",
            AddonVariableLevelUpSystemBox.SUB_XP_REMAINING => "xp_remaining",
            AddonVariableLevelUpSystemBox.SUB_IS_AT_MAX => "is_at_max",
            AddonVariableLevelUpSystemBox.SUB_MAX_LEVEL => "max_level",
            _ => "value"
        };

        // ── LevelProgress ────────────────────────────────────────────────────
        private class LevelProgress
        {
            public int CurrentLevel { get; }
            public int CurrentXp { get; }
            public int ProgressXp { get; }
            public int ProgressPercent { get; }
            public int TotalXpRequired { get; }
            public int XpRemaining { get; }
            public bool IsAtMax { get; }
            public int MaxLevel { get; }

            public LevelProgress(int currentLevel, int currentXp, int progressXp, int progressPercent,
                int totalXpRequired, int xpRemaining, bool isAtMax, int maxLevel)
            {
                CurrentLevel = currentLevel;
                CurrentXp = currentXp;
                ProgressXp = progressXp;
                ProgressPercent = progressPercent;
                TotalXpRequired = totalXpRequired;
                XpRemaining = xpRemaining;
                IsAtMax = isAtMax;
                MaxLevel = maxLevel;
            }
        }

        private static LevelProgress CalculateProgress(AddonVariableLevelUpSystemBox levelSystem, int rawBaseValue)
        {
            int currentXp = Math.Max(0, rawBaseValue);
            var entries = BuildThresholdEntries(levelSystem);

            if (entries.Count == 0) entries.Add(new LevelEntry(1, 0));

            int maxLevel = entries[^1].Level;
            int currentLevel = 1;
            int currentThreshold = 0;
            int nextThreshold = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (currentXp >= entry.RequiredXp)
                {
                    currentLevel = entry.Level;
                    currentThreshold = entry.RequiredXp;
                    nextThreshold = (i + 1 < entries.Count) ? entries[i + 1].RequiredXp : entry.RequiredXp;
                }
                else
                {
                    nextThreshold = entry.RequiredXp;
                    break;
                }
            }

            bool isAtMax = currentLevel >= maxLevel;
            if (isAtMax) nextThreshold = currentThreshold;

            int progressXp = Math.Max(0, currentXp - currentThreshold);
            int delta = Math.Max(0, nextThreshold - currentThreshold);
            int progressPercent = isAtMax ? 100 : (delta <= 0 ? 100 : Math.Max(0, Math.Min(100, (int)Math.Floor(progressXp * 100.0 / delta))));
            int totalXpRequired = isAtMax ? currentThreshold : nextThreshold;
            int xpRemaining = Math.Max(0, totalXpRequired - currentXp);

            return new LevelProgress(currentLevel, currentXp, progressXp, progressPercent, totalXpRequired, xpRemaining, isAtMax, maxLevel);
        }

        private static List<LevelEntry> BuildThresholdEntries(AddonVariableLevelUpSystemBox levelSystem) =>
            levelSystem.Mode switch
            {
                AddonVariableLevelUpSystemBox.MODE_EXPONENTIAL => BuildExponentialEntries(levelSystem),
                AddonVariableLevelUpSystemBox.MODE_MANUAL => BuildManualEntries(levelSystem),
                _ => BuildLinearEntries(levelSystem)
            };

        private static List<LevelEntry> BuildLinearEntries(AddonVariableLevelUpSystemBox ls)
        {
            var result = new List<LevelEntry>();
            int maxLevel = Math.Max(1, ls.MaxLevel);
            int step = Math.Max(0, ls.StepSize);

            for (int level = 1; level <= maxLevel; level++)
                result.Add(new LevelEntry(level, Clamp((long)(level - 1) * step)));

            return result;
        }

        private static List<LevelEntry> BuildExponentialEntries(AddonVariableLevelUpSystemBox ls)
        {
            var result = new List<LevelEntry>();
            int maxLevel = Math.Max(1, ls.MaxLevel);
            long increment = Math.Max(0, ls.FirstLevelXp);
            int factor = Math.Max(0, ls.IncreaseFactor);
            long threshold = 0;

            result.Add(new LevelEntry(1, 0));

            for (int level = 2; level <= maxLevel; level++)
            {
                threshold += increment;
                result.Add(new LevelEntry(level, Clamp(threshold)));
                increment = Clamp((long)Math.Round(increment * (100.0 + factor) / 100.0));
            }

            return result;
        }

        private static List<LevelEntry> BuildManualEntries(AddonVariableLevelUpSystemBox ls)
        {
            var anchors = ParseAnchors(ls.InterpolationText);
            if (!anchors.ContainsKey(1)) anchors[1] = 0;

            var sorted = anchors.OrderBy(kvp => kvp.Key).ToList();
            if (sorted.Count == 0) return new List<LevelEntry> { new LevelEntry(1, 0) };

            var result = new SortedDictionary<int, int>();

            for (int i = 0; i < sorted.Count; i++)
            {
                int currentLevel = Math.Max(1, sorted[i].Key);
                int currentXp = Math.Max(0, sorted[i].Value);
                result[currentLevel] = currentXp;

                if (i + 1 >= sorted.Count) continue;

                int nextLevel = Math.Max(currentLevel, sorted[i + 1].Key);
                int nextXp = Math.Max(0, sorted[i + 1].Value);

                if (nextLevel <= currentLevel) continue;

                for (int level = currentLevel + 1; level < nextLevel; level++)
                {
                    double ratio = (double)(level - currentLevel) / (nextLevel - currentLevel);
                    result[level] = Clamp((long)Math.Round(currentXp + (nextXp - currentXp) * ratio));
                }
            }

            return result.Select(kvp => new LevelEntry(kvp.Key, kvp.Value)).ToList();
        }

        private static Dictionary<int, int> ParseAnchors(string text)
        {
            var result = new Dictionary<int, int>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            foreach (var rawLine in text.Split('\n'))
            {
                string line = rawLine?.Trim() ?? "";
                if (string.IsNullOrEmpty(line)) continue;

                int sep = line.IndexOf('=');
                if (sep < 0) sep = line.IndexOf(',');
                if (sep <= 0) continue;

                if (!int.TryParse(line.Substring(0, sep).Trim(), out int level)) continue;
                if (!int.TryParse(line.Substring(sep + 1).Trim(), out int xp)) continue;
                if (level <= 0 || xp < 0) continue;

                result[level] = xp;
            }
            return result;
        }

        private static int Clamp(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
    }

    // Alias para que compile sin cambiar los callers
    public class Integer
    {
        public int? Value { get; }
        public Integer(int? v) { Value = v; }
        public static implicit operator Integer(int? v) => new Integer(v);
        public static implicit operator int?(Integer i) => i?.Value;
    }
}