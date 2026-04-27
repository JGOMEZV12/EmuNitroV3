using Polar.HabboHotel.Items.Wired;
using System.Collections.Generic;
using System.Linq;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonAnimationTimeBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonAnimationTime;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public const int CODE = 60;
        public const int MIN_DURATION_MS = 50;
        public const int MAX_DURATION_MS = 2000;
        private const int DEFAULT_DURATION = 500; // Reemplaza WiredMovementsComposer.DEFAULT_DURATION

        private int _durationMs = DEFAULT_DURATION;
        public int DurationMs => _durationMs;

        public AddonAnimationTimeBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

                public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            for (int i = 0; i < paramsCount; i++) packet.PopInt();

            this.StringData = packet.PopString();

            if (this.SetItems != null) this.SetItems.Clear();
            int itemsCount = packet.PopInt();
            for (int i = 0; i < itemsCount; i++)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (item != null) this.SetItems.TryAdd(item.Id, item);
            }

            int delay = packet.PopInt();
            if (this is IWiredCycle cycle) cycle.Delay = delay;
        }

                                public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(100);
            packet.WriteInteger(SetItems?.Count ?? 0);
            foreach (var item in SetItems?.Values.ToList() ?? new List<Item>()) packet.WriteInteger(item.Id);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? "");
            packet.WriteInteger(0); // Params count
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(this is IWiredCycle cycle ? cycle.Delay : 0);
        }

        public void LoadData(string wiredData)
        {
            OnPickUp();

            if (string.IsNullOrEmpty(wiredData))
                return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonSerializer.Deserialize<JsonData>(wiredData);
                _durationMs = NormalizeDuration(data?.DurationMs ?? DEFAULT_DURATION);
                return;
            }

            // Fallback: dato crudo como entero
            if (int.TryParse(wiredData, out int parsed))
                _durationMs = NormalizeDuration(parsed);
            else
                _durationMs = DEFAULT_DURATION;
        }

        public void OnPickUp()
        {
            _durationMs = DEFAULT_DURATION;
        }

        public bool Execute(params object[] @params)
        {
            return false;
        }

        private static int NormalizeDuration(int value) =>
            Math.Max(MIN_DURATION_MS, Math.Min(MAX_DURATION_MS, value));

        private class JsonData
        {
            public int DurationMs { get; set; }

            public JsonData() { }
            public JsonData(int durationMs) => DurationMs = durationMs;
        }
    }
}