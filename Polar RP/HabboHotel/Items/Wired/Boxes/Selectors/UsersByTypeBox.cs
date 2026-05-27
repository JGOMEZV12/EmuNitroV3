using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersByTypeBox : IWiredItem, IWiredCustomData
    {
        private const int ENTITY_HABBO = 1;
        private const int ENTITY_PET   = 2;
        private const int ENTITY_BOT   = 4;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersByType;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int  entityType    = ENTITY_HABBO;
        private bool filterExisting = false;
        private bool invert         = false;
        private int  delay          = 0;

        public UsersByTypeBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();
            Packet.PopString();
            this.delay = Packet.PopInt();

            this.entityType     = NormalizeEntityType(intParams.Length > 0 ? intParams[0] : ENTITY_HABBO);
            this.filterExisting = intParams.Length > 1 && intParams[1] == 1;
            this.invert         = intParams.Length > 2 && intParams[2] == 1;
        }

        public string GetWiredData() => JsonConvert.SerializeObject(new JsonData { entityType = this.entityType, filterExisting = this.filterExisting, invert = this.invert, delay = this.delay });

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;
            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;
            this.entityType     = NormalizeEntityType(data.entityType);
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false); Packet.WriteInteger(0); Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId); Packet.WriteInteger(Item.Id);
            Packet.WriteString(""); Packet.WriteInteger(3);
            Packet.WriteInteger(this.entityType);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(0); Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay); Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData() { this.entityType = ENTITY_HABBO; this.filterExisting = false; this.invert = false; this.delay = 0; }

        private static int NormalizeEntityType(int v)
        {
            switch (v) { case ENTITY_PET: case ENTITY_BOT: return v; default: return ENTITY_HABBO; }
        }

        private class JsonData { public int entityType { get; set; } public bool filterExisting { get; set; } public bool invert { get; set; } public int delay { get; set; } }
    }
}
