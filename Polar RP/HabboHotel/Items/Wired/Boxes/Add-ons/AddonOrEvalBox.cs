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
    class AddonOrEvalBox : IWiredItem, IWiredCustomData
    {
        public const int CODE = 66;
        public const int MODE_ALL = 0;
        public const int MODE_AT_LEAST_ONE = 1;
        public const int MODE_NOT_ALL = 2;
        public const int MODE_NONE = 3;
        public const int MODE_LESS_THAN = 4;
        public const int MODE_EXACTLY = 5;
        public const int MODE_MORE_THAN = 6;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonOrEval;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int _evaluationMode = MODE_ALL;
        private int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private int _compareValue = 1;

        public int EvaluationMode => _evaluationMode;
        public int FurniSource => _furniSource;
        public int CompareValue => _compareValue;

        public AddonOrEvalBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            // intParams → string → furnis
            int paramsCount = packet.PopInt();
            int rawMode = paramsCount > 0 ? packet.PopInt() : MODE_ALL;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawCompare = paramsCount > 2 ? packet.PopInt() : 1;

            string strParam = packet.PopString();

            int furniCount = packet.PopInt();
            SetItems.Clear();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            Console.WriteLine($"[AddonOrEvalBox] HandleSave — mode={rawMode}, furniSource={rawSource}, compare={rawCompare}, furniCount={furniCount}");

            _evaluationMode = NormalizeEvaluationMode(rawMode);
            _furniSource = rawSource;
            _compareValue = NormalizeCompareValue(rawCompare);
            StringData = $"{_evaluationMode};{_compareValue}";
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                evaluationMode = this._evaluationMode,
                furniSource = this._furniSource,
                compareValue = this._compareValue,
                itemIds = SetItems.Keys.ToList()
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _evaluationMode = MODE_ALL;
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            _compareValue = 1;
            SetItems.Clear();

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _evaluationMode = NormalizeEvaluationMode(data.evaluationMode);
                _furniSource = data.furniSource;
                _compareValue = NormalizeCompareValue(data.compareValue);

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }
            }
            else
            {
                // Retrocompatibilidad: "mode;furniSource;compareValue" o "mode\t..."
                var parts = wiredData.Split(new[] { ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (parts.Length > 0) _evaluationMode = NormalizeEvaluationMode(int.Parse(parts[0]));
                    if (parts.Length > 1) _furniSource = int.Parse(parts[1]);
                    if (parts.Length > 2) _compareValue = NormalizeCompareValue(int.Parse(parts[2]));
                }
                catch
                {
                    _evaluationMode = MODE_ALL;
                    _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
                    _compareValue = 1;
                }
            }

            StringData = $"{_evaluationMode};{_compareValue}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(_evaluationMode);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_compareValue);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        public bool Evaluate(List<IWiredItem> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;

            int matched = conditions.Count(c => c.Execute());
            int total = conditions.Count;

            return MatchesMode(_evaluationMode, matched, total, _compareValue);
        }

        public static bool MatchesMode(int evaluationMode, int matched, int total, int compareValue)
        {
            if (total <= 0) return true;

            return NormalizeEvaluationMode(evaluationMode) switch
            {
                MODE_AT_LEAST_ONE => matched > 0,
                MODE_NOT_ALL => matched > 0 && matched < total,
                MODE_NONE => matched == 0,
                MODE_LESS_THAN => matched < NormalizeCompareValue(compareValue),
                MODE_EXACTLY => matched == NormalizeCompareValue(compareValue),
                MODE_MORE_THAN => matched > NormalizeCompareValue(compareValue),
                _ => matched >= total // MODE_ALL
            };
        }

        private static int NormalizeEvaluationMode(int value) =>
            value is MODE_ALL or MODE_AT_LEAST_ONE or MODE_NOT_ALL
                  or MODE_NONE or MODE_LESS_THAN or MODE_EXACTLY or MODE_MORE_THAN
                ? value : MODE_ALL;

        private static int NormalizeCompareValue(int value) =>
            Math.Max(0, Math.Min(100, value));

        private class JsonData
        {
            public int evaluationMode { get; set; }
            public int furniSource { get; set; }
            public int compareValue { get; set; }
            public List<int> itemIds { get; set; }
        }
    }
}