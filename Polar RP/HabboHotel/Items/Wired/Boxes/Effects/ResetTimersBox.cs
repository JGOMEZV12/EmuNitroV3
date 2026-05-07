using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class ResetTimersBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectResetTimers;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get => _delay;
            set { _delay = value; TickCount = value + 1; }
        }

        public int TickCount { get; set; }

        private int _delay = 0;

        public ResetTimersBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            packet.PopInt();    // intCount = 0
            packet.PopString(); // string vacío
            Delay = packet.PopInt();
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData { delay = Delay });
        }

        public void LoadWiredData(string wiredData)
        {
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data != null)
                    Delay = data.delay;
            }
            else
            {
                // Retrocompatibilidad: era directamente el número
                if (int.TryParse(wiredData, out int delay))
                    Delay = delay;
            }

            TickCount = Delay;
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(0); // sin furnis
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(1);
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            TickCount = Delay;
            return true;
        }

        public bool OnCycle()
        {
            foreach (var wiredItem in Instance.GetWired().GetAllItems())
            {
                if (wiredItem is IWiredCycle cycle)
                    cycle.TickCount = cycle.Delay + 1;
            }
            return true;
        }

        private class JsonData
        {
            public int delay { get; set; }
        }
    }
}