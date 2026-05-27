using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Selectors
{
    class FurniNeighborhoodBox : IWiredItem, IWiredCustomData
    {
        private const int SOURCE_USER_TRIGGER  = 0;
        private const int SOURCE_USER_SIGNAL   = 1;
        private const int SOURCE_USER_CLICKED  = 2;
        private const int SOURCE_FURNI_TRIGGER = 3;
        private const int SOURCE_FURNI_PICKED  = 4;
        private const int SOURCE_FURNI_SIGNAL  = 5;

        private const int MAX_PICKED_FURNI = 20;
        private const int MAX_TILE_OFFSETS = 64;

        public Room   Instance { get; set; }
        public Item   Item     { get; set; }
        public WiredBoxType Type => WiredBoxType.SelectorFurniNeighborhood;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool   BoolData   { get; set; }
        public string ItemsData  { get; set; }

        private int             sourceType      = SOURCE_USER_TRIGGER;
        private bool            filterExisting  = false;
        private bool            invert          = false;
        private int             targetOffsetX   = 0;
        private int             targetOffsetY   = 0;
        private List<int[]>     tileOffsets     = new List<int[]>();
        private List<int>       pickedFurniIds  = new List<int>();
        private int             delay           = 0;

        public FurniNeighborhoodBox(Room Instance, Item Item)
        {
            this.Instance  = Instance;
            this.Item      = Item;
            this.SetItems  = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            // intParams layout: [sourceType, filterExisting, invert, targetOffsetX, targetOffsetY, n, x0, y0, x1, y1, …]
            int paramCount  = Packet.PopInt();
            int[] intParams = new int[paramCount];
            for (int i = 0; i < paramCount; i++) intParams[i] = Packet.PopInt();

            Packet.PopString(); // unused

            int furniCount = Packet.PopInt();
            this.pickedFurniIds = new List<int>();
            for (int i = 0; i < furniCount && i < MAX_PICKED_FURNI; i++)
                this.pickedFurniIds.Add(Packet.PopInt());

            this.delay = Packet.PopInt();

            this.sourceType     = intParams.Length > 0 ? intParams[0] : SOURCE_USER_TRIGGER;
            this.filterExisting = intParams.Length > 1 && intParams[1] == 1;
            this.invert         = intParams.Length > 2 && intParams[2] == 1;
            this.targetOffsetX  = intParams.Length > 3 ? intParams[3] : 0;
            this.targetOffsetY  = intParams.Length > 4 ? intParams[4] : 0;

            this.tileOffsets = new List<int[]>();
            if (intParams.Length > 5)
            {
                int n = intParams[5];
                for (int i = 0; i < n && i < MAX_TILE_OFFSETS; i++)
                {
                    int xi = 6 + i * 2;
                    if (xi + 1 < intParams.Length)
                        this.tileOffsets.Add(new int[] { intParams[xi], intParams[xi + 1] });
                }
            }
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                sourceType     = this.sourceType,
                filterExisting = this.filterExisting,
                invert         = this.invert,
                targetOffsetX  = this.targetOffsetX,
                targetOffsetY  = this.targetOffsetY,
                tileOffsets    = this.tileOffsets,
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

            this.sourceType     = data.sourceType;
            this.filterExisting = data.filterExisting;
            this.invert         = data.invert;
            this.targetOffsetX  = data.targetOffsetX;
            this.targetOffsetY  = data.targetOffsetY;
            this.tileOffsets    = data.tileOffsets    ?? new List<int[]>();
            this.pickedFurniIds = data.pickedFurniIds ?? new List<int>();
            this.delay          = data.delay;
        }

        public void Serialize(ServerPacket Packet)
        {
            bool pickMode = (this.sourceType == SOURCE_FURNI_PICKED);

            Packet.WriteBoolean(pickMode);
            Packet.WriteInteger(pickMode ? MAX_PICKED_FURNI : 0);

            if (pickMode && this.pickedFurniIds.Count > 0)
            {
                Packet.WriteInteger(this.pickedFurniIds.Count);
                foreach (int id in this.pickedFurniIds) Packet.WriteInteger(id);
            }
            else
            {
                Packet.WriteInteger(0);
            }

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");

            int paramCount = 6 + this.tileOffsets.Count * 2;
            Packet.WriteInteger(paramCount);
            Packet.WriteInteger(this.sourceType);
            Packet.WriteInteger(this.filterExisting ? 1 : 0);
            Packet.WriteInteger(this.invert         ? 1 : 0);
            Packet.WriteInteger(this.targetOffsetX);
            Packet.WriteInteger(this.targetOffsetY);
            Packet.WriteInteger(this.tileOffsets.Count);
            foreach (int[] offset in this.tileOffsets)
            {
                Packet.WriteInteger(offset[0]);
                Packet.WriteInteger(offset[1]);
            }

            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.delay);
            Packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params) => false;

        private void ResetData()
        {
            this.sourceType     = SOURCE_USER_TRIGGER;
            this.filterExisting = false;
            this.invert         = false;
            this.targetOffsetX  = 0;
            this.targetOffsetY  = 0;
            this.tileOffsets    = new List<int[]>();
            this.pickedFurniIds = new List<int>();
            this.delay          = 0;
        }

        private class JsonData
        {
            public int          sourceType     { get; set; }
            public bool         filterExisting { get; set; }
            public bool         invert         { get; set; }
            public int          targetOffsetX  { get; set; }
            public int          targetOffsetY  { get; set; }
            public List<int[]>  tileOffsets    { get; set; }
            public List<int>    pickedFurniIds { get; set; }
            public int          delay          { get; set; }
        }
    }
}
