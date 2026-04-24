using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Utilities;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class CollisionCaseBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type { get { return WiredBoxType.EffectCollisionCase; } }
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        public int Delay
        {
            get { return this._delay; }
            set
            {
                this._delay = value;
                this.TickCount = value + 1;
            }
        }

        public int TickCount { get; set; }
        private bool Requested;
        private int _delay = 0;
        private long _next = 0;

        public CollisionCaseBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "0";
            this.TickCount = Delay;
            this.Requested = false;
        }

        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();
            for (int i = 0; i < IntCount; i++)
                Packet.PopInt(); // consumir sin guardar

            string StringParam = Packet.PopString();

            int FurniCount = Packet.PopInt();
            SetItems.Clear();
            for (int i = 0; i < FurniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(Packet.PopInt());
                if (selected != null && !Instance.GetWired().OtherBoxHasItem(this, selected.Id))
                    SetItems.TryAdd(selected.Id, selected);
            }

            this.Delay = Packet.PopInt();
            int SelectionCode = Packet.PopInt();
            this.StringData = SelectionCode.ToString(); // ✅ guardar SelectionCode, no FurniSource
        }

        public void Serialize(ServerPacket Packet)
        {
            int furniSource = 0;
            if (!string.IsNullOrEmpty(this.StringData))
                int.TryParse(this.StringData, out furniSource);

            Packet.WriteBoolean(false);
            Packet.WriteInteger(100);
            Packet.WriteInteger(SetItems.Count);
            foreach (Item item in SetItems.Values.ToList())
                Packet.WriteInteger(item.Id);

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);
            Packet.WriteInteger(Item.Id);
            Packet.WriteString("");
            Packet.WriteInteger(1);    // intParams count
            Packet.WriteInteger(0);    // intParams[0] = no se usa
            Packet.WriteInteger(furniSource); // ✅ stuffTypeSelectionCode = el valor real

            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            Packet.WriteInteger(this.Delay);
            Packet.WriteInteger(0);
        }
        public bool Execute(params object[] Params)
        {
            if (this.SetItems.Count == 0)
                return false;

            if (this._next == 0 || this._next < PolarEnvironment.Now())
                this._next = PolarEnvironment.Now() + this.Delay;

            if (!Requested)
            {
                this.TickCount = this.Delay;
                this.Requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null || !Requested || _next == 0)
                return false;

            long Now = PolarEnvironment.Now();
            if (_next < Now)
            {
                foreach (Item Item in this.SetItems.Values.ToList())
                {
                    if (Item == null)
                        continue;

                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(Item))
                        continue;

                    Item toRemove = null;

                    if (Instance.GetWired().OtherBoxHasItem(this, Item.Id))
                        this.SetItems.TryRemove(Item.Id, out toRemove);

                    Point Point = Instance.GetGameMap().GetChaseMovement(Item);
                    Instance.GetWired().OnUserFurniCollision(Instance, Item);

                    if (!Instance.GetGameMap().ItemCanMove(Item, Point))
                        continue;

                    if (Instance.GetGameMap().CanRollItemHere(Point.X, Point.Y) && !Instance.GetGameMap().SquareHasUsers(Point.X, Point.Y))
                    {
                        Double NewZ = Item.GetZ;
                        Boolean CanBePlaced = true;

                        var Items = Instance.GetGameMap().GetCoordinatedItems(Point);
                        foreach (Item IItem in Items.ToList())
                        {
                            if (IItem == null || IItem.Id == Item.Id)
                                continue;

                            if (!IItem.GetBaseItem().Walkable)
                            {
                                _next = 0;
                                CanBePlaced = false;
                                break;
                            }

                            if (IItem.TotalHeight > NewZ)
                                NewZ = IItem.TotalHeight;

                            if (CanBePlaced && !IItem.GetBaseItem().Stackable)
                                CanBePlaced = false;
                        }

                        if (CanBePlaced && Point != Item.Coordinate)
                        {
                            Instance.SendMessage(new SlideObjectBundleComposer(Item.GetX, Item.GetY, Item.GetZ, Point.X, Point.Y, NewZ, 0, 0, Item.Id));
                            Instance.GetRoomItemHandler().SetFloorItem(Item, Point.X, Point.Y, NewZ);
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