using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonFilterUserBox : IWiredItem, IWiredCustomData
    {
        private const int MAX_FILTER_AMOUNT = 10000;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonFilterUser;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int amount = 0;
        public int Amount => amount;

        public AddonFilterUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawAmount = paramsCount > 0 ? packet.PopInt() : 0;

            string strParam = packet.PopString();

            if (rawAmount == 0 && !string.IsNullOrEmpty(strParam) &&
                int.TryParse(strParam, out int parsed))
                rawAmount = parsed;

            Console.WriteLine($"[AddonFilterUserBox] HandleSave — amount={rawAmount}");

            this.amount = NormalizeAmount(rawAmount);
            this.StringData = this.amount.ToString();
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData { amount = this.amount });
        }

        public void LoadWiredData(string wiredData)
        {
            this.amount = 0;
            this.StringData = "0";

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                this.amount = NormalizeAmount(data?.amount ?? 0);
            }
            else
            {
                if (int.TryParse(wiredData, out int old))
                    this.amount = NormalizeAmount(old);
            }

            this.StringData = this.amount.ToString();
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(1);
            packet.WriteInteger(this.amount);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params) => true;

        private static int NormalizeAmount(int value) =>
            Math.Max(0, Math.Min(MAX_FILTER_AMOUNT, value));

        private class JsonData
        {
            public int amount { get; set; }
        }
    }
}