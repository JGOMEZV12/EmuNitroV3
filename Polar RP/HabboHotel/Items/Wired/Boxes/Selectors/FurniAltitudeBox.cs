using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class FurniAltitudeBox : IWiredItem, IWiredCustomData
    {
        private const int COMPARISON_LESS    = 0;
        private const int COMPARISON_EQUAL   = 1;
        private const int COMPARISON_GREATER = 2;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniAltitude;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int    comparison     = COMPARISON_EQUAL;
        private double altitude       = 0.0;
        private bool   filterExisting = false;
        private bool   invert         = false;
        private int    delay          = 0;

        public FurniAltitudeBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [comparison, filterExisting, invert]
            // stringParam: altitude value
            // delay
            int paramCount   = Packet.PopInt();
            int[] intParams  = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            string strParam  = Packet.PopString();
            this.delay       = Packet.PopInt();

            this.comparison     = (intParams.Length > 0) ? NormalizeComparison(intParams[0]) : COMPARISON_EQUAL;
            this.filterExisting = (intParams.Length > 1) && intParams[1] == 1;
            this.invert         = (intParams.Length > 2) && intParams[2] == 1;
            this.altitude       = ParseAltitude(strParam);
            this.StringData     = FormatAltitude(this.altitude);
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                comparison     = this.comparison,
                altitude       = FormatAltitude(this.altitude),
                filterExisting = this.filterExisting,
                invert         = this.invert,
                delay          = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.comparison     = NormalizeComparison(data.comparison);
            this.altitude       = ParseAltitude(data.altitude);
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.delay          = data.delay;
            this.StringData     = FormatAltitude(this.altitude);
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(FormatAltitude(this.altitude));
            Packet.WriteInteger(3);
            Packet.WriteInteger(this.comparison);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert ? 1 : 0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        // ── helpers ──────────────────────────────────────────────────────────
        private void ResetData()
        {
            this.comparison     = COMPARISON_EQUAL;
            this.altitude       = 0.0;
            this.filterExisting = false;
            this.invert         = false;
            this.delay          = 0;
            this.StringData     = "";
        }

        private static int NormalizeComparison(int value)
        {
            if (value < COMPARISON_LESS || value > COMPARISON_GREATER) return COMPARISON_EQUAL;
            return value;
        }

        private static double ParseAltitude(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            if (double.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double result))
                return Math.Round(Math.Max(0.0, Math.Min(40.0, result)), 2);
            return 0.0;
        }

        private static string FormatAltitude(double value)
        {
            return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ── JSON DTO ─────────────────────────────────────────────────────────
        private class JsonData
        {
            public int    comparison     { get; set; }
            public string altitude       { get; set; }
            public bool   filterExisting { get; set; }
            public bool   invert         { get; set; }
            public int    delay          { get; set; }
        }
    }
}
