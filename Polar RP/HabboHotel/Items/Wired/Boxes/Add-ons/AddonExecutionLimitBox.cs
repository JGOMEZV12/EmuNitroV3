using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonExecutionLimitBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonExecutionLimit;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }
        private int _triggerCount = 0;
        private long _lastReset = 0;

        public AddonExecutionLimitBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket Packet)
        {
            int unknown = Packet.PopInt();
            int limit = Packet.PopInt();
            this.StringData = limit.ToString();
        }

        
        public void Serialize(ServerPacket Packet)
        {
            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item Item in SetItems.Values.ToList())
            {
                Packet.WriteInteger(Item.Id);
            }
            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString(StringData);
            Packet.WriteInteger(0);
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(0);
        }
        public bool Execute(params object[] @params)
        {
            if (string.IsNullOrEmpty(StringData)) return true;
            if (int.TryParse(StringData, out int limit))
            {
                long now = PolarEnvironment.Now();
                if (now - _lastReset > 1000) { _lastReset = now; _triggerCount = 0; }
                if (_triggerCount >= limit) return false;
                _triggerCount++;
            }
            return true;
        }
    }
}