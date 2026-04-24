using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonOrEvalBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonOrEval;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public const int CODE = 66;
        public const int MODE_ALL = 0;
        public const int MODE_AT_LEAST_ONE = 1;
        public const int MODE_NOT_ALL = 2;
        public const int MODE_NONE = 3;
        public const int MODE_LESS_THAN = 4;
        public const int MODE_EXACTLY = 5;
        public const int MODE_MORE_THAN = 6;

        private int _evaluationMode = MODE_ALL;
        private int _compareValue = 1;

        public int EvaluationMode => _evaluationMode;
        public int CompareValue => _compareValue;

        public AddonOrEvalBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            _evaluationMode = NormalizeEvaluationMode(packet.PopInt());
            _compareValue = NormalizeCompareValue(packet.PopInt());
            StringData = $"{_evaluationMode};{_compareValue}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems.Count);
            foreach (var item in SetItems.Values.ToList())
                packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(_evaluationMode);
            packet.WriteInteger(0); // furniSource — no aplica en C#
            packet.WriteInteger(_compareValue);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            return true; // Marker for Triggers
        }

        /// <summary>
        /// Evalúa cuántas condiciones de la lista se cumplen y aplica el modo configurado.
        /// Llamar desde el trigger pasando la lista de condiciones ya evaluadas.
        /// </summary>
        public bool Evaluate(List<IWiredItem> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

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
    }
}