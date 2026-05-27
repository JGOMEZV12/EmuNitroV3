using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class FurniOnFurniBox : IWiredItem, IWiredCustomData
    {
        private const int SELECT_FURNI_ABOVE       = 0;
        private const int SELECT_FURNI_BELOW       = 1;
        private const int SELECT_FURNI_SAME_HEIGHT = 2;
        private const int SELECT_ALL_FURNI_ON_TILE = 3;

        // furniSource constants (mirrors WiredSourceUtil)
        private const int SOURCE_TRIGGER  = 0;
        private const int SOURCE_SELECTED = 1;
        private const int SOURCE_SELECTOR = 2;
        private const int SOURCE_SIGNAL   = 3;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniOnFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int       selectionType  = SELECT_FURNI_ABOVE;
        private int       furniSource    = SOURCE_TRIGGER;
        private bool      filterExisting = false;
        private bool      invert         = false;
        private List<int> itemIds        = new List<int>();
        private int       delay          = 0;

        public FurniOnFurniBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [selectionType, furniSource, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            Packet.PopString(); // unused string param

            int furniCount = Packet.PopInt();
            this.itemIds = new List<int>();
            for (int i = 0; i < furniCount; i++) this.itemIds.Add(Packet.PopInt());

            this.delay = Packet.PopInt();

            this.selectionType  = NormalizeSelectionType(intParams.Length > 0 ? intParams[0] : SELECT_FURNI_ABOVE);
            this.furniSource    = NormalizeFurniSource(intParams.Length > 1 ? intParams[1] : SOURCE_TRIGGER);
            this.filterExisting = intParams.Length > 2 && intParams[2] == 1;
            this.invert         = intParams.Length > 3 && intParams[3] == 1;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                selectionType  = this.selectionType,
                furniSource    = this.furniSource,
                filterExisting = this.filterExisting,
                invert         = this.invert,
                itemIds        = this.itemIds,
                delay          = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.selectionType  = NormalizeSelectionType(data.selectionType);
            this.furniSource    = NormalizeFurniSource(data.furniSource);
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.itemIds        = data.itemIds ?? new List<int>();
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(20); // MAXIMUM_FURNI_SELECTION
            Packet.WriteInteger(this.itemIds.Count);
            foreach (int id in this.itemIds) Packet.WriteInteger(id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(4);
            Packet.WriteInteger(this.selectionType);
            Packet.WriteInteger(this.furniSource);
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
            this.selectionType  = SELECT_FURNI_ABOVE;
            this.furniSource    = SOURCE_TRIGGER;
            this.filterExisting = false;
            this.invert         = false;
            this.itemIds        = new List<int>();
            this.delay          = 0;
        }

        private static int NormalizeSelectionType(int value)
        {
            if (value < SELECT_FURNI_ABOVE || value > SELECT_ALL_FURNI_ON_TILE) return SELECT_FURNI_ABOVE;
            return value;
        }

        private static int NormalizeFurniSource(int value)
        {
            switch (value)
            {
                case SOURCE_SELECTED:
                case SOURCE_SELECTOR:
                case SOURCE_SIGNAL:
                case SOURCE_TRIGGER:
                    return value;
                default:
                    return SOURCE_TRIGGER;
            }
        }

        private class JsonData
        {
            public int       selectionType  { get; set; }
            public int       furniSource    { get; set; }
            public bool      filterExisting { get; set; }
            public bool      invert         { get; set; }
            public List<int> itemIds        { get; set; }
            public int       delay          { get; set; }
        }
    }
}
