using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using Polar.HabboHotel.Rooms.Instance;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MoveAndRotateBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMoveAndRotate;
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

        private int _delay;
        private long _next;
        private bool _requested;
        private int _direction;
        private int _rotation;
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;

        public MoveAndRotateBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
        }

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            _direction = intCount > 0 ? packet.PopInt() : 0;
            _rotation = intCount > 1 ? packet.PopInt() : 0;
            _furniSource = intCount > 2 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null && !Instance.GetWired().OtherBoxHasItem(this, selected.Id))
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                direction = _direction,
                rotation = _rotation,
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _direction = 0;
            _rotation = 0;
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _direction = data.direction;
                _rotation = data.rotation;
                _furniSource = data.furniSource;
                Delay = data.delay;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "direction\trotation\tdelay\tid1\rid2"
                var parts = wiredData.Split('\t');
                if (parts.Length == 4)
                {
                    int.TryParse(parts[0], out _direction);
                    int.TryParse(parts[1], out _rotation);
                    if (int.TryParse(parts[2], out int delay)) Delay = delay;

                    foreach (var s in parts[3].Split('\r'))
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        if (!int.TryParse(s, out int id)) continue;

                        var item = Instance.GetRoomItemHandler().GetItem(id);
                        if (item != null)
                            SetItems.TryAdd(item.Id, item);
                    }
                }

                _furniSource = SetItems.Count == 0
                    ? WiredSourceUtil.SOURCE_TRIGGER
                    : WiredSourceUtil.SOURCE_SELECTED;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        public void Serialize(ServerPacket packet)
        {
            var toRemove = SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(_direction);
            packet.WriteInteger(_rotation);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (SetItems.Count == 0) return false;

            if (_next == 0 || _next < DateTime.UtcNow.Ticks)
                _next = DateTime.UtcNow.Ticks + Delay;

            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null || !_requested || _next == 0)
                return false;

            if (_next < DateTime.UtcNow.Ticks)
            {
                foreach (var item in SetItems.Values.ToList())
                {
                    if (item == null) continue;
                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(item)) continue;
                    if (Instance.GetWired().OtherBoxHasItem(this, item.Id))
                    {
                        SetItems.TryRemove(item.Id, out _);
                        continue;
                    }

                    var point = HandleMovement(_direction, new Point(item.GetX, item.GetY));
                    var newRot = HandleRotation(_rotation, item.Rotation);

                    Instance.GetWired().OnUserFurniCollision(Instance, item);

                    if (!Instance.GetGameMap().ItemCanMove(item, point)) continue;

                    if (Instance.GetGameMap().CanRollItemHere(point.X, point.Y) && !Instance.GetGameMap().SquareHasUsers(point.X, point.Y))
                    {
                        var newZ = Instance.GetGameMap().GetHeightForSquareFromData(point);
                        var canBePlaced = true;

                        foreach (var coordinatedItem in Instance.GetGameMap().GetCoordinatedItems(point).ToList())
                        {
                            if (coordinatedItem == null || coordinatedItem.Id == item.Id) continue;

                            if (!coordinatedItem.GetBaseItem().Walkable)
                            {
                                _next = 0;
                                canBePlaced = false;
                                break;
                            }

                            if (coordinatedItem.TotalHeight > newZ) newZ = coordinatedItem.TotalHeight;
                            if (!coordinatedItem.GetBaseItem().Stackable) canBePlaced = false;
                        }

                        if (newRot != item.Rotation)
                        {
                            item.Rotation = newRot;
                            item.UpdateState(false, true);
                        }

                        if (canBePlaced && point != item.Coordinate)
                        {
                            Instance.SendMessage(new SlideObjectBundleComposer(
                                item.GetX, item.GetY, item.GetZ,
                                point.X, point.Y, newZ, 0, 0, item.Id));
                            Instance.GetRoomItemHandler().SetFloorItem(item, point.X, point.Y, newZ);
                        }
                    }
                }

                _next = 0;
                _requested = false;
                return true;
            }
            return false;
        }

        private int HandleRotation(int mode, int rotation)
        {
            if (rotation < 0 || rotation > 6) rotation = 0;
            switch (mode)
            {
                case 1: rotation += 2; if (rotation > 6) rotation = 0; break;
                case 2: rotation -= 2; if (rotation < 0) rotation = 6; break;
                case 3:
                    if (Random.Shared.Next(0, 2) == 0) { rotation += 2; if (rotation > 6) rotation = 0; }
                    else { rotation -= 2; if (rotation < 0) rotation = 6; }
                    break;
            }
            return rotation;
        }

        private Point HandleMovement(int mode, Point position)
        {
            int maxX = Instance.GetGameMap().Model.MapSizeX;
            int maxY = Instance.GetGameMap().Model.MapSizeY;
            Point newPos;

            switch (mode)
            {
                case 0: newPos = position; break;
                case 1:
                    switch (Random.Shared.Next(1, 5))
                    {
                        case 1: newPos = new Point(position.X + 1, position.Y); break;
                        case 2: newPos = new Point(position.X - 1, position.Y); break;
                        case 3: newPos = new Point(position.X, position.Y + 1); break;
                        default: newPos = new Point(position.X, position.Y - 1); break;
                    }
                    break;
                case 2: newPos = Random.Shared.Next(0, 2) == 0 ? new Point(position.X - 1, position.Y) : new Point(position.X + 1, position.Y); break;
                case 3: newPos = Random.Shared.Next(0, 2) == 0 ? new Point(position.X, position.Y - 1) : new Point(position.X, position.Y + 1); break;
                case 4: newPos = new Point(position.X, position.Y - 1); break;
                case 5: newPos = new Point(position.X + 1, position.Y); break;
                case 6: newPos = new Point(position.X, position.Y + 1); break;
                case 7: newPos = new Point(position.X - 1, position.Y); break;
                default: newPos = position; break;
            }

            if (newPos.X < 0 || newPos.Y < 0 || newPos.X >= maxX || newPos.Y >= maxY)
                return position;

            return newPos;
        }

        private class JsonData
        {
            public int direction { get; set; }
            public int rotation { get; set; }
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
        }
    }
}