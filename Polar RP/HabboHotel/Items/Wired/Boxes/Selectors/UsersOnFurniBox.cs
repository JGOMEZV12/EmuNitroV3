using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersOnFurniBox : IWiredItem, IWiredCustomData
    {
        // furniSource constants (mirrors WiredSourceUtil)
        private const int SOURCE_TRIGGER  = 0;
        private const int SOURCE_SELECTED = 1;
        private const int SOURCE_SELECTOR = 2;
        private const int SOURCE_SIGNAL   = 3;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersOnFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int       furniSource    = SOURCE_TRIGGER;
        private bool      filterExisting = false;
        private bool      invert         = false;
        private List<int> itemIds        = new List<int>();
        private int       delay          = 0;

        public UsersOnFurniBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [furniSource, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            Packet.PopString(); // unused

            int furniCount = Packet.PopInt();
            this.itemIds = new List<int>();
            for (int i = 0; i < furniCount; i++) this.itemIds.Add(Packet.PopInt());

            this.delay = Packet.PopInt();

            this.furniSource    = NormalizeFurniSource(intParams.Length > 0 ? intParams[0] : SOURCE_TRIGGER);
            this.filterExisting = intParams.Length > 1 && intParams[1] == 1;
            this.invert         = intParams.Length > 2 && intParams[2] == 1;

            // Si hay furni seleccionado manualmente, override source
            if (this.itemIds.Count > 0 && this.furniSource == SOURCE_TRIGGER)
                this.furniSource = SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
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

            Packet.WriteInteger(Item.GetBaseItem().SpriteId); Packet.WriteInteger(Item.Id);
            Packet.WriteString(""); Packet.WriteInteger(3);
            Packet.WriteInteger(this.furniSource);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(0); Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay); Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.furniSource    = SOURCE_TRIGGER;
            this.filterExisting = false;
            this.invert         = false;
            this.itemIds        = new List<int>();
            this.delay          = 0;
        }

        private static int NormalizeFurniSource(int v)
        {
            switch (v)
            {
                case SOURCE_SELECTED:
                case SOURCE_SELECTOR:
                case SOURCE_SIGNAL:
                case SOURCE_TRIGGER:
                    return v;
                default:
                    return SOURCE_TRIGGER;
            }
        }

        private class JsonData
        {
            public int       furniSource    { get; set; }
            public bool      filterExisting { get; set; }
            public bool      invert         { get; set; }
            public List<int> itemIds        { get; set; }
            public int       delay          { get; set; }
        }
    }
}
