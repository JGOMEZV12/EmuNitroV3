using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Incoming;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class TeleportBotToFurniBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectTeleportBotToFurniBox; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public TeleportBotToFurniBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
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
        public bool Execute(params object[] Params)
        {
            if (String.IsNullOrEmpty(this.StringData))
                return false;

            RoomUser User = this.Instance.GetRoomUserManager().GetBotByName(this.StringData);
            if (User == null)
                return false;

            Random rand = new Random();
            List<Item> Items = SetItems.Values.ToList();
            Items = Items.OrderBy(x => rand.Next()).ToList();

            if (Items.Count == 0)
                return false;

            Item Item = Items.First();
            if (Item == null)
                return false;

            if (!Instance.GetRoomItemHandler().GetFloor.Contains(Item))
            {
                SetItems.TryRemove(Item.Id, out Item);

                if (Items.Contains(Item))
                    Items.Remove(Item);

                if (SetItems.Count == 0 || Items.Count == 0)
                    return false;

                Item = Items.First();
                if (Item == null)
                    return false;
            }

            if (this.Instance.GetGameMap() == null)
                return false;

            this.Instance.GetGameMap().TeleportToItem(User, Item);
            this.Instance.GetRoomUserManager().UpdateUserStatusses();

            return true;
        }
    }
}
