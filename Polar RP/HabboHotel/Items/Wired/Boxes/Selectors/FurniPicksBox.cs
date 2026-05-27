using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class FurniPicksBox : IWiredItem, IWiredCustomData
    {
        private const int MAX_PICKED_FURNI = 20;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniPicks;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private bool      filterExisting = false;
        private bool      invert         = false;
        private List<int> pickedFurniIds = new List<int>();
        private int       delay          = 0;

        public FurniPicksBox(Room Instance, Item Item)
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

            Packet.PopString(); // unused

            int furniCount = Packet.PopInt();
            this.pickedFurniIds = new List<int>();
            for (int i = 0; i < furniCount && i < MAX_PICKED_FURNI; i++)
                this.pickedFurniIds.Add(Packet.PopInt());

            this.delay = Packet.PopInt();

            this.filterExisting = intParams.Length > 0 && intParams[0] == 1;
            this.invert         = intParams.Length > 1 && intParams[1] == 1;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                filterExisting = this.filterExisting,
                invert         = this.invert,
                pickedFurniIds = this.pickedFurniIds,
                delay          = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.pickedFurniIds = data.pickedFurniIds ?? new List<int>();
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(true);
            Packet.WriteInteger(MAX_PICKED_FURNI);
            Packet.WriteInteger(this.pickedFurniIds.Count);
            foreach (int id in this.pickedFurniIds) Packet.WriteInteger(id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
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
            this.filterExisting = false;
            this.invert         = false;
            this.pickedFurniIds = new List<int>();
            this.delay          = 0;
        }

        private class JsonData
        {
            public bool      filterExisting { get; set; }
            public bool      invert         { get; set; }
            public List<int> pickedFurniIds { get; set; }
            public int       delay          { get; set; }
        }
    }
}
