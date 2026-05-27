using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class UsersByActionBox : IWiredItem, IWiredCustomData
    {
        // Action type IDs (mirrors WiredUserActionType)
        private const int ACTION_WAVE     = 1;
        private const int ACTION_BLOW_KISS = 2;
        private const int ACTION_LAUGH    = 3;
        private const int ACTION_AWAKE    = 4;
        private const int ACTION_RELAX    = 5;
        private const int ACTION_SIT      = 6;
        private const int ACTION_STAND    = 7;
        private const int ACTION_LAY      = 8;
        private const int ACTION_SIGN     = 9;
        private const int ACTION_DANCE    = 10;
        private const int ACTION_THUMB_UP = 11;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorUsersByAction;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int  selectedAction    = ACTION_WAVE;
        private bool signFilterEnabled = false;
        private int  signId            = 0;
        private bool danceFilterEnabled = false;
        private int  danceId           = 1;
        private bool filterExisting    = false;
        private bool invert            = false;
        private int  delay             = 0;

        public UsersByActionBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams: [selectedAction, signFilterEnabled, signId, danceFilterEnabled, danceId, filterExisting, invert]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            Packet.PopString(); // unused
            this.delay = Packet.PopInt();

            this.selectedAction     = NormalizeAction(intParams.Length > 0 ? intParams[0] : ACTION_WAVE);
            this.signFilterEnabled  = intParams.Length > 1 && intParams[1] == 1;
            this.signId             = NormalizeSignId(intParams.Length > 2 ? intParams[2] : 0);
            this.danceFilterEnabled = intParams.Length > 3 && intParams[3] == 1;
            this.danceId            = NormalizeDanceId(intParams.Length > 4 ? intParams[4] : 1);
            this.filterExisting     = intParams.Length > 5 && intParams[5] == 1;
            this.invert             = intParams.Length > 6 && intParams[6] == 1;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                selectedAction    = this.selectedAction,
                signFilterEnabled = this.signFilterEnabled,
                signId            = this.signId,
                danceFilterEnabled = this.danceFilterEnabled,
                danceId           = this.danceId,
                filterExisting    = this.filterExisting,
                invert            = this.invert,
                delay             = this.delay
            });
        }

        public void LoadWiredData(string wiredData)
        {
            ResetData();
            if (string.IsNullOrEmpty(wiredData) || !wiredData.StartsWith("{")) return;

            var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
            if (data == null) return;

            this.selectedAction     = NormalizeAction(data.selectedAction);
            this.signFilterEnabled  = data.signFilterEnabled;
            this.signId             = NormalizeSignId(data.signId);
            this.danceFilterEnabled = data.danceFilterEnabled;
            this.danceId            = NormalizeDanceId(data.danceId);
            this.filterExisting     = data.filterExisting;
            this.invert             = data.invert;
            this.delay              = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(7);
            Packet.WriteInteger(this.selectedAction);
            Packet.WriteInteger(this.signFilterEnabled  ? 1 : 0);
            Packet.WriteInteger(this.signId);
            Packet.WriteInteger(this.danceFilterEnabled ? 1 : 0);
            Packet.WriteInteger(this.danceId);
            Packet.WriteInteger(this.filterExisting     ? 1 : 0);
            Packet.WriteInteger(this.invert             ? 1 : 0);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.selectedAction     = ACTION_WAVE;
            this.signFilterEnabled  = false;
            this.signId             = 0;
            this.danceFilterEnabled = false;
            this.danceId            = 1;
            this.filterExisting     = false;
            this.invert             = false;
            this.delay              = 0;
        }

        private static int NormalizeAction(int v)
        {
            switch (v)
            {
                case ACTION_WAVE:
                case ACTION_BLOW_KISS:
                case ACTION_LAUGH:
                case ACTION_AWAKE:
                case ACTION_RELAX:
                case ACTION_SIT:
                case ACTION_STAND:
                case ACTION_LAY:
                case ACTION_SIGN:
                case ACTION_DANCE:
                case ACTION_THUMB_UP:
                    return v;
                default:
                    return ACTION_WAVE;
            }
        }

        private static int NormalizeSignId(int v)  => (v < 0 || v > 17) ? 0 : v;
        private static int NormalizeDanceId(int v) => (v < 1 || v > 4)  ? 1 : v;

        private class JsonData
        {
            public int  selectedAction     { get; set; }
            public bool signFilterEnabled  { get; set; }
            public int  signId             { get; set; }
            public bool danceFilterEnabled { get; set; }
            public int  danceId            { get; set; }
            public bool filterExisting     { get; set; }
            public bool invert             { get; set; }
            public int  delay              { get; set; }
        }
    }
}
