using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonMoveCarryUsersBox : IWiredItem, IWiredCustomData
    {
        public const int MODE_DIRECTLY_ON_FURNI = 0;
        public const int MODE_SAME_TILE = 1;
        public const int SOURCE_ALL_ROOM_USERS = 900;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonMoveCarryUsers;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int carryMode = MODE_DIRECTLY_ON_FURNI;
        private int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

        public int CarryMode => carryMode;
        public int UserSource => userSource;

        public AddonMoveCarryUsersBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawCarryMode = paramsCount > 0 ? packet.PopInt() : MODE_DIRECTLY_ON_FURNI;
            int rawSource = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;

            string strParam = packet.PopString();

            Console.WriteLine($"[AddonMoveCarryUsersBox] HandleSave — carryMode={rawCarryMode}, userSource={rawSource}");

            this.carryMode = NormalizeCarryMode(rawCarryMode);
            this.userSource = NormalizeUserSource(rawSource);
            this.StringData = $"{carryMode}\t{userSource}";
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                carryMode = this.carryMode,
                userSource = this.userSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.carryMode = MODE_DIRECTLY_ON_FURNI;
            this.userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.carryMode = NormalizeCarryMode(data.carryMode);
                this.userSource = NormalizeUserSource(data.userSource);
            }
            else
            {
                // Retrocompatibilidad: "carryMode\tuserSource"
                var parts = wiredData.Split('\t');
                if (parts.Length > 0 && int.TryParse(parts[0], out int cm))
                    this.carryMode = NormalizeCarryMode(cm);
                if (parts.Length > 1 && int.TryParse(parts[1], out int us))
                    this.userSource = NormalizeUserSource(us);
            }

            this.StringData = $"{carryMode}\t{userSource}";
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(2);
            packet.WriteInteger(this.carryMode);
            packet.WriteInteger(this.userSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        private static int NormalizeCarryMode(int value) =>
            value == MODE_SAME_TILE ? MODE_SAME_TILE : MODE_DIRECTLY_ON_FURNI;

        private static int NormalizeUserSource(int value) =>
            value == SOURCE_ALL_ROOM_USERS ||
            value == WiredBoxTypeUtility.SOURCE_SELECTOR ||
            value == WiredBoxTypeUtility.SOURCE_TRIGGER
                ? value
                : WiredBoxTypeUtility.SOURCE_TRIGGER;

        private class JsonData
        {
            public int carryMode { get; set; }
            public int userSource { get; set; }
        }
    }
}