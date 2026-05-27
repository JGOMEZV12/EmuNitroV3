using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersHandItemBox : IWiredItem, IWiredCustomData
    {
        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersHandItem;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int  handItemId    = 0;
        private bool filterExisting = false;
        private bool invert         = false;
        private int  delay          = 0;

        public UsersHandItemBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [handItemId, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();
            Packet.PopString();
            this.delay = Packet.PopInt();

            this.handItemId     = System.Math.Max(0, intParams.Length > 0 ? intParams[0] : 0);
            this.filterExisting = intParams.Length > 1 && intParams[1] == 1;
            this.invert         = intParams.Length > 2 && intParams[2] == 1;
        }

        public string GetWiredData() => JsonConvert.SerializeObject(new JsonData { handItemId = this.handItemId, filterExisting = this.filterExisting, invert = this.invert, delay = this.delay });

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;
            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;
            this.handItemId     = System.Math.Max(0, data.handItemId);
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false); Packet.WriteInteger(0); Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId); Packet.WriteInteger(Item.Id);
            Packet.WriteString(""); Packet.WriteInteger(3);
            Packet.WriteInteger(this.handItemId);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(0); Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay); Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData() { this.handItemId = 0; this.filterExisting = false; this.invert = false; this.delay = 0; }

        private class JsonData { public int handItemId { get; set; } public bool filterExisting { get; set; } public bool invert { get; set; } public int delay { get; set; } }
    }
}
