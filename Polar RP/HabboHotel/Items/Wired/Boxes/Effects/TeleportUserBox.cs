using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class TeleportUserBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectTeleportToFurni; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public int Delay { get { return this._delay; } set { this._delay = value; this.TickCount = value + 1; } }
        public int TickCount { get; set; }
        public string ItemsData { get; set; }
        private Queue _queue;
        private int _delay;

        public TeleportUserBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this._queue = new Queue();
            this.TickCount = Delay;
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

        public bool OnCycle()
        {
            if (_queue.Count == 0 || SetItems.Count == 0)
            {
                this._queue.Clear();
                this.TickCount = Delay;
                return true;
            }
            while (_queue.Count > 0)
            {
                Habbo Player = (Habbo)_queue.Dequeue();
                if (Player == null || Player.CurrentRoom != Instance) continue;
                this.TeleportUser(Player);
            }
            this.TickCount = Delay;
            return true;
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
            if (Params == null || Params.Length == 0) return false;
            Habbo Player = (Habbo)Params[0];
            if (Player == null) return false;
            if (Player.Effects() != null) Player.Effects().ApplyEffect(EffectsList.Twinkle);
            this._queue.Enqueue(Player);
            return true;
        }

        private void TeleportUser(Habbo Player)
        {
            if (Player == null) return;
            Room Room = Player.CurrentRoom;
            if (Room == null) return;
            RoomUser User = Player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(Player.Id);
            if (User == null) return;
            if (Player.IsTeleporting || Player.IsHopping || Player.TeleporterId != 0) return;
            Random rand = new Random();
            List<Item> Items = SetItems.Values.ToList();
            Items = Items.OrderBy(x => rand.Next()).ToList();
            if (Items.Count == 0) return;
            Item Item = Items.First();
            if (Item == null) return;
            if (!Instance.GetRoomItemHandler().GetFloor.Contains(Item))
            {
                SetItems.TryRemove(Item.Id, out Item);
                if (Items.Contains(Item)) Items.Remove(Item);
                if (SetItems.Count == 0 || Items.Count == 0) return;
                Item = Items.First();
                if (Item == null) return;
            }
            if (Room.GetGameMap() == null) return;
            Room.GetGameMap().TeleportToItem(User, Item);
            Room.GetRoomUserManager().UpdateUserStatusses();
            if (Player.Effects() != null) Player.Effects().ApplyEffect(0);
        }
    }
}