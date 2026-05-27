using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersGroupBox : IWiredItem, IWiredCustomData
    {
        private const int GROUP_CURRENT_ROOM = 0;
        private const int GROUP_SELECTED     = 1;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersGroup;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int  groupType       = GROUP_CURRENT_ROOM;
        private int  selectedGroupId = 0;
        private bool filterExisting  = false;
        private bool invert          = false;
        private int  delay           = 0;

        public UsersGroupBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [groupType, selectedGroupId, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();
            Packet.PopString();
            this.delay = Packet.PopInt();

            this.groupType       = NormalizeGroupType(intParams.Length > 0 ? intParams[0] : GROUP_CURRENT_ROOM);
            this.selectedGroupId = System.Math.Max(0, intParams.Length > 1 ? intParams[1] : 0);
            this.filterExisting  = intParams.Length > 2 && intParams[2] == 1;
            this.invert          = intParams.Length > 3 && intParams[3] == 1;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                groupType       = this.groupType,
                selectedGroupId = this.selectedGroupId,
                filterExisting  = this.filterExisting,
                invert          = this.invert,
                delay           = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;
            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;
            this.groupType       = NormalizeGroupType(data.groupType);
            this.selectedGroupId = System.Math.Max(0, data.selectedGroupId);
            this.filterExisting  = data.filterExisting;
            this.invert          = data.invert;
            this.delay           = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false); Packet.WriteInteger(0); Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId); Packet.WriteInteger(Item.Id);
            Packet.WriteString(""); Packet.WriteInteger(4);
            Packet.WriteInteger(this.groupType);
            Packet.WriteInteger(this.selectedGroupId);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(0); Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay); Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.groupType = GROUP_CURRENT_ROOM; this.selectedGroupId = 0;
            this.filterExisting = false; this.invert = false; this.delay = 0;
        }

        private static int NormalizeGroupType(int v) => v == GROUP_SELECTED ? GROUP_SELECTED : GROUP_CURRENT_ROOM;

        private class JsonData
        {
            public int  groupType       { get; set; }
            public int  selectedGroupId { get; set; }
            public bool filterExisting  { get; set; }
            public bool invert          { get; set; }
            public int  delay           { get; set; }
        }
    }
}
