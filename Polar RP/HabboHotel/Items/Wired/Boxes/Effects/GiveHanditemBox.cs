using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items.Wired;
using Polar.HabboHotel.Items;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class GiveHanditemBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectGiveHanditem;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }
        public GiveHanditemBox(Room instance, Item item) { Instance = instance; Item = item; SetItems = new ConcurrentDictionary<int, Item>(); StringData = "0"; }
                public void HandleSave(ClientPacket packet)
        {
            int unknown = packet.PopInt();
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
            packet.WriteInteger(1);
            packet.WriteInteger(int.TryParse(StringData, out int val) ? val : 0);
            packet.WriteInteger(0); // Categorical
            packet.WriteInteger(0); // Delay or Selection
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
        }
        public bool Execute(params object[] @params) { /* Logic */ return true; }
    }
}