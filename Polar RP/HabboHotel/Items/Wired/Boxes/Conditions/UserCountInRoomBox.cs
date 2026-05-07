using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    class UserCountInRoomBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionUserCountInRoom;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        protected int lowerLimit = 0;
        protected int upperLimit = 50;
        protected int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

        public UserCountInRoomBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "0;50";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawLower = paramsCount > 0 ? packet.PopInt() : 0;
            int rawUpper = paramsCount > 1 ? packet.PopInt() : 50;
            int rawSource = paramsCount > 2 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;

            string strParam = packet.PopString();

            Console.WriteLine($"[UserCountInRoomBox] HandleSave — lower={rawLower}, upper={rawUpper}, userSource={rawSource}");

            this.lowerLimit = rawLower;
            this.upperLimit = rawUpper;
            this.userSource = rawSource;
            this.StringData = $"{lowerLimit};{upperLimit}";
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                lowerLimit = this.lowerLimit,
                upperLimit = this.upperLimit,
                userSource = this.userSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.lowerLimit = 0;
            this.upperLimit = 50;
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.lowerLimit = data.lowerLimit;
                this.upperLimit = data.upperLimit;
                this.userSource = data.userSource;
            }
            else
            {
                // Retrocompatibilidad: "lower:upper"
                var parts = wiredData.Split(':');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out this.lowerLimit);
                    int.TryParse(parts[1], out this.upperLimit);
                }
                this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            }

            this.StringData = $"{lowerLimit};{upperLimit}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(this.lowerLimit);
            packet.WriteInteger(this.upperLimit);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            int count = Instance.UserCount;
            return count >= this.lowerLimit && count <= this.upperLimit;
        }

        protected class JsonData
        {
            public int lowerLimit { get; set; }
            public int upperLimit { get; set; }
            public int userSource { get; set; }
        }
    }
}