using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MatchPositionBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMatchPosition;
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
        private bool _requested;
        private bool _state;
        private bool _direction;
        private bool _position;
        private bool _altitude;
        private int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private List<FurniSetting> _settings = new List<FurniSetting>();

        public MatchPositionBox(Room instance, Item item)
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
            _settings.Clear();

            int intCount = packet.PopInt();
            _state = intCount > 0 && packet.PopInt() == 1;
            _direction = intCount > 1 && packet.PopInt() == 1;
            _position = intCount > 2 && packet.PopInt() == 1;
            _altitude = intCount > 3 && packet.PopInt() == 1;
            _furniSource = intCount > 4 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            for (int i = 5; i < intCount; i++) packet.PopInt();

            packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected == null) continue;

                SetItems.TryAdd(selected.Id, selected);
                _settings.Add(new FurniSetting
                {
                    itemId = selected.Id,
                    x = selected.GetX,
                    y = selected.GetY,
                    z = selected.GetZ,
                    rotation = selected.Rotation,
                    extraData = selected.ExtraData ?? ""
                });
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                state = _state,
                direction = _direction,
                position = _position,
                altitude = _altitude,
                furniSource = _furniSource,
                delay = Delay,
                items = _settings
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _settings.Clear();
            _state = false;
            _direction = false;
            _position = false;
            _altitude = false;
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _state = data.state;
                _direction = data.direction;
                _position = data.position;
                _altitude = data.altitude;
                _furniSource = data.furniSource;
                Delay = data.delay;

                foreach (var setting in data.items ?? new List<FurniSetting>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(setting.itemId);
                    if (item == null) continue;

                    SetItems.TryAdd(item.Id, item);
                    _settings.Add(setting);
                }

                if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                    _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: "itemCount:id-x-y-z-rot-state;...:state:dir:pos:delay"
                var parts = wiredData.Split(':');
                if (parts.Length >= 6)
                {
                    foreach (var s in parts[1].Split(';'))
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        var f = s.Split('-');
                        if (f.Length < 5) continue;
                        if (!int.TryParse(f[0], out int id)) continue;

                        var item = Instance.GetRoomItemHandler().GetItem(id);
                        if (item == null) continue;

                        SetItems.TryAdd(item.Id, item);
                        _settings.Add(new FurniSetting
                        {
                            itemId = id,
                            extraData = f[1],
                            rotation = int.TryParse(f[2], out int rot) ? rot : 0,
                            x = int.TryParse(f[3], out int x) ? x : 0,
                            y = int.TryParse(f[4], out int y) ? y : 0,
                            z = f.Length > 5 && double.TryParse(f[5],
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture,
                                            out double z) ? z : 0
                        });
                    }

                    _state = parts[2] == "1";
                    _direction = parts[3] == "1";
                    _position = parts[4] == "1";
                    _altitude = false;
                    if (int.TryParse(parts[5], out int delay)) Delay = delay;
                    _furniSource = _settings.Count == 0
                        ? WiredBoxTypeUtility.SOURCE_TRIGGER
                        : WiredBoxTypeUtility.SOURCE_SELECTED;
                }
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        public void Serialize(ServerPacket packet)
        {
            // Limpiar items que ya no están en la sala
            var toRemove = _settings.Where(s => Instance.GetRoomItemHandler().GetItem(s.itemId) == null).ToList();
            foreach (var s in toRemove)
            {
                _settings.Remove(s);
                SetItems.TryRemove(s.itemId, out _);
            }

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(_settings.Count);
            foreach (var s in _settings)
                packet.WriteInteger(s.itemId);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(5);
            packet.WriteInteger(_state ? 1 : 0);
            packet.WriteInteger(_direction ? 1 : 0);
            packet.WriteInteger(_position ? 1 : 0);
            packet.WriteInteger(_altitude ? 1 : 0);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (!_requested || _settings.Count == 0)
            {
                _requested = false;
                return false;
            }

            foreach (var setting in _settings)
            {
                Item item = Instance.GetRoomItemHandler().GetItem(setting.itemId);
                if (item == null) continue;

                if (_state)
                    SetState(item, setting.extraData);

                if (_direction && !_position)
                    SetRotation(item, setting.rotation);

                if (_altitude && !_position)
                    SetPosition(item, item.GetX, item.GetY, setting.z);

                if (_position)
                {
                    double targetZ = _altitude ? setting.z : item.GetZ;
                    int targetRot = _direction ? setting.rotation : item.Rotation;
                    SetPosition(item, setting.x, setting.y, targetZ);
                    if (_direction) SetRotation(item, targetRot);
                }
            }

            _requested = false;
            return true;
        }

        private void SetState(Item item, string extradata)
        {
            if (item.ExtraData == extradata) return;
            if (item.GetBaseItem().InteractionType == InteractionType.DICE) return;
            item.ExtraData = extradata;
            item.UpdateState(false, true);
        }

        private void SetRotation(Item item, int rotation)
        {
            if (item.Rotation == rotation) return;
            item.Rotation = rotation;
            item.UpdateState(false, true);
        }

        private void SetPosition(Item item, int x, int y, double z)
        {
            Instance.SendMessage(new SlideObjectBundleComposer(
                item.GetX, item.GetY, item.GetZ, x, y, z, 0, 0, item.Id));
            Instance.GetRoomItemHandler().SetFloorItem(item, x, y, z);
        }

        private class JsonData
        {
            public bool state { get; set; }
            public bool direction { get; set; }
            public bool position { get; set; }
            public bool altitude { get; set; }
            public int furniSource { get; set; }
            public int delay { get; set; }
            public List<FurniSetting> items { get; set; }
        }
    }
}