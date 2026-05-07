using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MoveFurniToUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMoveFurniToNearestUser;
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
        private long _next = 0;
        private bool _requested = false;
        private int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

        public MoveFurniToUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
        }

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            int fSource = intCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            for (int i = 1; i < intCount; i++) packet.PopInt();

            packet.PopString();

            SetItems.Clear();
            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null && !Instance.GetWired().OtherBoxHasItem(this, selected.Id))
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();
            _furniSource = fSource;

            if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Delay = data.delay;
                _furniSource = data.furniSource;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                    _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "delay\tid1;id2;"
                var parts = wiredData.Split('\t');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int delay))
                    Delay = delay;

                if (parts.Length == 2 && parts[1].Contains(";"))
                {
                    foreach (var s in parts[1].Split(';'))
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        if (!int.TryParse(s, out int id)) continue;

                        var item = Instance.GetRoomItemHandler().GetItem(id);
                        if (item != null)
                            SetItems.TryAdd(item.Id, item);
                    }
                }

                _furniSource = SetItems.Count == 0
                    ? WiredBoxTypeUtility.SOURCE_TRIGGER
                    : WiredBoxTypeUtility.SOURCE_SELECTED;
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
            packet.WriteInteger(1);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (SetItems.Count == 0) return false;

            if (_next == 0 || _next < PolarEnvironment.Now())
                _next = PolarEnvironment.Now() + Delay;

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

            if (_next < PolarEnvironment.Now())
            {
                foreach (Item item in SetItems.Values.ToList())
                {
                    if (item == null) continue;
                    if (!Instance.GetRoomItemHandler().GetFloor.Contains(item)) continue;
                    if (Instance.GetWired().OtherBoxHasItem(this, item.Id))
                    {
                        SetItems.TryRemove(item.Id, out _);
                        continue;
                    }

                    Point point = Instance.GetGameMap().GetChaseMovement(item);
                    Instance.GetWired().OnUserFurniCollision(Instance, item);

                    if (!Instance.GetGameMap().ItemCanMove(item, point)) continue;

                    if (Instance.GetGameMap().CanRollItemHere(point.X, point.Y) && !Instance.GetGameMap().SquareHasUsers(point.X, point.Y))
                    {
                        double newZ = item.GetZ;
                        bool canBePlaced = true;

                        foreach (Item iItem in Instance.GetGameMap().GetCoordinatedItems(point).ToList())
                        {
                            if (iItem == null || iItem.Id == item.Id) continue;

                            if (!iItem.GetBaseItem().Walkable)
                            {
                                _next = 0;
                                canBePlaced = false;
                                break;
                            }

                            if (iItem.TotalHeight > newZ) newZ = iItem.TotalHeight;
                            if (!iItem.GetBaseItem().Stackable) canBePlaced = false;
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

        private class JsonData
        {
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
        }
    }
}