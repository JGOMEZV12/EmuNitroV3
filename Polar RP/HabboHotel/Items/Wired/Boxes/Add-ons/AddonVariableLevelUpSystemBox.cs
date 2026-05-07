using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonVariableLevelUpSystemBox : IWiredItem, IWiredCustomData
    {
        public const int MODE_LINEAR = 1;
        public const int MODE_EXPONENTIAL = 2;
        public const int MODE_MANUAL = 3;
        public const int SUB_CURRENT_LEVEL = 0;
        public const int SUB_CURRENT_XP = 1;
        public const int SUB_LEVEL_PROGRESS = 2;
        public const int SUB_LEVEL_PROGRESS_PCT = 3;
        public const int SUB_TOTAL_XP_REQUIRED = 4;
        public const int SUB_XP_REMAINING = 5;
        public const int SUB_IS_AT_MAX = 6;
        public const int SUB_MAX_LEVEL = 7;
        public const int SUBVARIABLE_COUNT = 8;

        private const int DEFAULT_STEP_SIZE = 100;
        private const int DEFAULT_MAX_LEVEL = 10;
        private const int DEFAULT_FIRST_LEVEL_XP = 100;
        private const int DEFAULT_INCREASE_FACTOR = 100;
        private const int MAX_MANUAL_TEXT_LENGTH = 4096;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonVariableLevelUpSystem;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int mode = MODE_LINEAR;
        private int stepSize = DEFAULT_STEP_SIZE;
        private int maxLevel = DEFAULT_MAX_LEVEL;
        private int firstLevelXp = DEFAULT_FIRST_LEVEL_XP;
        private int increaseFactor = DEFAULT_INCREASE_FACTOR;
        private string interpolationText = "";
        private int subvariableMask = (1 << SUB_CURRENT_LEVEL) | (1 << SUB_CURRENT_XP);

        public int Mode => mode;
        public int StepSize => stepSize;
        public int MaxLevel => maxLevel;
        public int FirstLevelXp => firstLevelXp;
        public int IncreaseFactor => increaseFactor;
        public string InterpolationText => interpolationText;

        public AddonVariableLevelUpSystemBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            // El cliente manda todo como string JSON en el strParam
            int paramsCount = packet.PopInt();
            // consumir int params si los hay
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            string strParam = packet.PopString();
            ApplyConfig(ParseJsonData(strParam));
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                mode = this.mode,
                stepSize = this.stepSize,
                maxLevel = this.maxLevel,
                firstLevelXp = this.firstLevelXp,
                increaseFactor = this.increaseFactor,
                interpolationText = this.interpolationText,
                subvariables = GetSelectedSubvariables()
            });
        }

        public void LoadWiredData(string wiredData)
        {
            Reset();
            if (string.IsNullOrEmpty(wiredData)) return;
            ApplyConfig(ParseJsonData(wiredData));
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(GetWiredData()); // Java serializa el JSON completo como string
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        public bool HasSubvariable(int subvariableType) =>
            subvariableType >= 0 && subvariableType < SUBVARIABLE_COUNT &&
            (subvariableMask & (1 << subvariableType)) != 0;

        public List<int> GetSelectedSubvariables()
        {
            var result = new List<int>();
            for (int i = 0; i < SUBVARIABLE_COUNT; i++)
                if (HasSubvariable(i)) result.Add(i);
            return result;
        }

        private void Reset()
        {
            mode = MODE_LINEAR;
            stepSize = DEFAULT_STEP_SIZE;
            maxLevel = DEFAULT_MAX_LEVEL;
            firstLevelXp = DEFAULT_FIRST_LEVEL_XP;
            increaseFactor = DEFAULT_INCREASE_FACTOR;
            interpolationText = "";
            subvariableMask = (1 << SUB_CURRENT_LEVEL) | (1 << SUB_CURRENT_XP);
        }

        private void ApplyConfig(JsonData data)
        {
            if (data == null) { Reset(); return; }

            mode = NormalizeMode(data.mode);
            stepSize = NormalizeNonNegative(data.stepSize, DEFAULT_STEP_SIZE);
            maxLevel = Math.Max(1, data.maxLevel > 0 ? data.maxLevel : DEFAULT_MAX_LEVEL);
            firstLevelXp = NormalizeNonNegative(data.firstLevelXp, DEFAULT_FIRST_LEVEL_XP);
            increaseFactor = NormalizeNonNegative(data.increaseFactor, DEFAULT_INCREASE_FACTOR);
            interpolationText = NormalizeInterpolationText(data.interpolationText);
            subvariableMask = NormalizeSubvariableMask(data.subvariables);
        }

        private static JsonData ParseJsonData(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new JsonData();

            try
            {
                if (value.TrimStart().StartsWith("{"))
                {
                    var data = JsonConvert.DeserializeObject<JsonData>(value);
                    return data ?? new JsonData();
                }
            }
            catch { }

            // Fallback: tratar como texto de interpolación manual
            return new JsonData { interpolationText = NormalizeInterpolationText(value), mode = MODE_MANUAL };
        }

        private static int NormalizeMode(int value) => value switch
        {
            MODE_EXPONENTIAL => MODE_EXPONENTIAL,
            MODE_MANUAL => MODE_MANUAL,
            _ => MODE_LINEAR
        };

        private static int NormalizeNonNegative(int value, int fallback) =>
            Math.Max(0, value > 0 ? value : fallback);

        private static string NormalizeInterpolationText(string value)
        {
            if (value == null) return "";
            string normalized = value.Replace("\r", "");
            return normalized.Length > MAX_MANUAL_TEXT_LENGTH
                ? normalized.Substring(0, MAX_MANUAL_TEXT_LENGTH)
                : normalized;
        }

        private static int NormalizeSubvariableMask(List<int> subvariables)
        {
            if (subvariables == null)
                return (1 << SUB_CURRENT_LEVEL) | (1 << SUB_CURRENT_XP);

            int mask = 0;
            foreach (int sub in subvariables)
                if (sub >= 0 && sub < SUBVARIABLE_COUNT)
                    mask |= (1 << sub);
            return mask;
        }

        private class JsonData
        {
            public int mode { get; set; } = MODE_LINEAR;
            public int stepSize { get; set; } = DEFAULT_STEP_SIZE;
            public int maxLevel { get; set; } = DEFAULT_MAX_LEVEL;
            public int firstLevelXp { get; set; } = DEFAULT_FIRST_LEVEL_XP;
            public int increaseFactor { get; set; } = DEFAULT_INCREASE_FACTOR;
            public string interpolationText { get; set; } = "";
            public List<int> subvariables { get; set; } = null;
        }
    }
}