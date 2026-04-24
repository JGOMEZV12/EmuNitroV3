using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Core;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MoveFurniToUserBox : IWiredItem, IWiredCycle
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }

        public WiredBoxType Type
        {
            get { return WiredBoxType.EffectMoveFurniToNearestUser; }
        }

        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }

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
        public string ItemsData { get; set; }
        private bool Requested;
        private int _delay = 0;
        private long _next = 0;

        public MoveFurniToUserBox(Room Instance, Item Item)
        {
            this.Instance = Instance;
            this.Item = Item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.TickCount = Delay;
            this.Requested = false;
        }

        // AMBAS CAJAS — HandleSave corregido:
        public void HandleSave(ClientPacket Packet)
        {
            int IntCount = Packet.PopInt();      // ints.length = 0 o 1
            for (int i = 0; i < IntCount; i++)
                Packet.PopInt();                 // consumir ints

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

            //Console.WriteLine($"[MoveFurniToUserBox] IntCount={IntCount} FurniCount={FurniCount} Delay={Delay} SelectionCode={SelectionCode}");
        }
        public void Serialize(ServerPacket Packet)
        {
            int furniSource = 0;
            if (!string.IsNullOrEmpty(this.StringData))
                int.TryParse(this.StringData, out furniSource);

            Packet.WriteBoolean(false);                              // stuffTypeSelectionEnabled
            Packet.WriteInteger(100);                                // furniLimit
            Packet.WriteInteger(SetItems.Count);                     // stuffIds count
            foreach (Item item in SetItems.Values.ToList())
                Packet.WriteInteger(item.Id);                        // stuffIds

            Packet.WriteInteger(Item.GetBaseItem().SpriteId);        // stuffTypeId
            Packet.WriteInteger(Item.Id);                            // id
            Packet.WriteString("");                                  // stringParam
            Packet.WriteInteger(1);                                  // intParams count
            Packet.WriteInteger(furniSource);                        // intParams[0] = furniSource
            Packet.WriteInteger(0);                                  // stuffTypeSelectionCode

            // WiredActionDefinition
            Packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type)); // type
            Packet.WriteInteger(this.Delay);                           // delayInPulses
            Packet.WriteInteger(0);                                    // conflictingTriggers count
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

                        List<Item> Items = Instance.GetGameMap().GetCoordinatedItems(Point);
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

                            if (CanBePlaced == true && !IItem.GetBaseItem().Stackable)
                                CanBePlaced = false;
                        }

                        if (CanBePlaced && Point != Item.Coordinate)
                        {
                            Instance.SendMessage(new SlideObjectBundleComposer(Item.GetX, Item.GetY, Item.GetZ, Point.X,
                                Point.Y, NewZ, 0, 0, Item.Id));
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
