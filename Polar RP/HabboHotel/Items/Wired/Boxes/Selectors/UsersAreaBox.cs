using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersAreaBox : IWiredItem, IWiredCustomData
    {
        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersArea;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int  rootX         = 0;
        private int  rootY         = 0;
        private int  areaWidth     = 0;
        private int  areaHeight    = 0;
        private bool filterExisting = false;
        private bool invert         = false;
        private int  delay          = 0;

        public UsersAreaBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [rootX, rootY, width, height, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            Packet.PopString(); // unused
            this.delay = Packet.PopInt();

            this.rootX          = intParams.Length > 0 ? intParams[0] : 0;
            this.rootY          = intParams.Length > 1 ? intParams[1] : 0;
            this.areaWidth      = intParams.Length > 2 ? intParams[2] : 0;
            this.areaHeight     = intParams.Length > 3 ? intParams[3] : 0;
            this.filterExisting = intParams.Length > 4 && intParams[4] == 1;
            this.invert         = intParams.Length > 5 && intParams[5] == 1;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                rootX          = this.rootX,
                rootY          = this.rootY,
                width          = this.areaWidth,
                height         = this.areaHeight,
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

            this.rootX          = data.rootX;
            this.rootY          = data.rootY;
            this.areaWidth      = data.width;
            this.areaHeight     = data.height;
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(6);
            Packet.WriteInteger(this.rootX);
            Packet.WriteInteger(this.rootY);
            Packet.WriteInteger(this.areaWidth);
            Packet.WriteInteger(this.areaHeight);
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
            this.rootX = this.rootY = this.areaWidth = this.areaHeight = 0;
            this.filterExisting = false;
            this.invert = false;
            this.delay = 0;
        }

        private class JsonData
        {
            public int  rootX          { get; set; }
            public int  rootY          { get; set; }
            public int  width          { get; set; }
            public int  height         { get; set; }
            public bool filterExisting { get; set; }
            public bool invert         { get; set; }
            public int  delay          { get; set; }
        }
    }
}
