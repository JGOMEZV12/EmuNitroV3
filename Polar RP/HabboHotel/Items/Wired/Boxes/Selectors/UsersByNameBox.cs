using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersByNameBox : IWiredItem, IWiredCustomData
    {
        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersByName;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private string      namesText      = "";
        private bool        filterExisting = false;
        private bool        invert         = false;
        private int         delay          = 0;

        public UsersByNameBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            string strParam = Packet.PopString();
            this.delay = Packet.PopInt();

            this.namesText      = NormalizeNamesText(strParam);
            this.filterExisting = intParams.Length > 0 && intParams[0] == 1;
            this.invert         = intParams.Length > 1 && intParams[1] == 1;
            this.StringData     = this.namesText;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                namesText      = this.namesText,
                filterExisting = this.filterExisting,
                invert         = this.invert,
                delay          = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;
                this.namesText      = NormalizeNamesText(data.namesText);
                this.filterExisting = data.filterExisting;
                this.invert         = data.invert;
                this.delay          = data.delay;
            }
            else
            {
                // Retrocompatibilidad: el dato era directamente la lista de nombres
                this.namesText = NormalizeNamesText(wiredData);
            }

            this.StringData = this.namesText;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(this.namesText);
            Packet.WriteInteger(2);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.namesText      = "";
            this.filterExisting = false;
            this.invert         = false;
            this.delay          = 0;
            this.StringData     = "";
        }

        private static string NormalizeNamesText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(l => l.Trim())
                             .Where(l => l.Length > 0)
                             .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join("\n", lines);
        }

        private class JsonData
        {
            public string namesText      { get; set; }
            public bool   filterExisting { get; set; }
            public bool   invert         { get; set; }
            public int    delay          { get; set; }
        }
    }
}
