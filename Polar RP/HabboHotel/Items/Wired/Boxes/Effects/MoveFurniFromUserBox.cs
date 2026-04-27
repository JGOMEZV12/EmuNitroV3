using Polar.HabboHotel.Items.Wired;
using Polar.Communication.Packets.Outgoing;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using System.Drawing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Utilities;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MoveFurniFromUserBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }

        public WiredBoxType Type => WiredBoxType.EffectMoveFurniFromNearestUser;

        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }

        private int _delay = 0;
        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                TickCount = value + 1;
            }
        }

        public int TickCount { get; set; }
        public string ItemsData { get; set; }
        private bool Requested;
        private long _next = 0;

        public MoveFurniFromUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new();
            TickCount = Delay;
            Requested = false;
        }

        // AMBAS CAJAS — HandleSave corregido:
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
            if (SetItems.Count == 0)
                return false;

            if (_next == 0 || _next < DateTime.UtcNow.Ticks)
                _next = DateTime.UtcNow.Ticks + Delay;

            if (!Requested)
            {
                TickCount = Delay;
                Requested = true;
            }

            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null || !Requested || _next == 0)
                return false;

            var now = DateTime.UtcNow.Ticks;
            if (_next < now)
            {
                foreach (Item item in SetItems.Values.ToList())
                {
                    if (item == null)
                        continue;

                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                        continue;

                    if (Instance.GetWired().OtherBoxHasItem(this, item.Id))
                        SetItems.TryRemove(item.Id, out _);

                    Point point = Instance.GetGameMap().GetChaseMovement(item);
                    Instance.GetWired().OnUserFurniCollision(Instance, item);

                    if (!Instance.GetGameMap().ItemCanMove(item, point))
                        continue;

                    if (Instance.GetGameMap().CanRollItemHere(point.X, point.Y) && !Instance.GetGameMap().SquareHasUsers(point.X, point.Y))
                    {
                        double newZ = item.GetZ;
                        bool canBePlaced = true;

                        List<Item> items = Instance.GetGameMap().GetCoordinatedItems(point);
                        foreach (Item iItem in items.ToList())
                        {
                            if (iItem == null || iItem.Id == item.Id)
                                continue;

                            if (!iItem.GetBaseItem().Walkable)
                            {
                                _next = 0;
                                canBePlaced = false;
                                break;
                            }

                            if (iItem.TotalHeight > newZ)
                                newZ = iItem.TotalHeight;

                            if (canBePlaced && !iItem.GetBaseItem().Stackable)
                                canBePlaced = false;
                        }

                        if (canBePlaced && point != item.Coordinate)
                        {
                            Instance.SendMessage(new SlideObjectBundleComposer(item.GetX, item.GetY, item.GetZ, point.X,
                                point.Y, newZ, 0, 0, item.Id));
                            Instance.GetRoomItemHandler().SetFloorItem(item, point.X, point.Y, newZ);
                        }
                    }
                }

                _next = 0;
                return true;
            }

            return false;
        }
    }
}
