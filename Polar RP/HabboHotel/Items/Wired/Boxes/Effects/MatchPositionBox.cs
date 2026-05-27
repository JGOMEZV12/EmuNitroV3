using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Concurrent;
using Polar.HabboHotel.Rooms.Instance;


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
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
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
            _furniSource = intCount > 4 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
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

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
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
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                // Deserializar como diccionario genérico primero para detectar el tipo de "items"
                var raw = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(wiredData);
                if (raw == null) return;

                // Leer campos escalares
                _state = raw["state"]?.Value<bool>() ?? false;
                _direction = raw["direction"]?.Value<bool>() ?? false;
                _position = raw["position"]?.Value<bool>() ?? false;
                _altitude = raw["altitude"]?.Value<bool>() ?? false;
                _furniSource = raw["furniSource"]?.Value<int>() ?? WiredSourceUtil.SOURCE_TRIGGER;
                Delay = raw["delay"]?.Value<int>() ?? 0;

                var itemsToken = raw["items"];

                if (itemsToken != null && itemsToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    // Formato viejo: items es un string "id:x,y,z,rot,state;"
                    ParseLegacyItemsString(itemsToken.Value<string>());
                }
                else if (itemsToken != null && itemsToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    // Formato nuevo: items es un array de objetos
                    var settings = itemsToken.ToObject<List<FurniSetting>>();
                    foreach (var setting in settings ?? new List<FurniSetting>())
                    {
                        var item = Instance.GetRoomItemHandler().GetItem(setting.itemId);
                        if (item == null) continue;
                        SetItems.TryAdd(item.Id, item);
                        _settings.Add(setting);
                    }
                }

                if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else
            {
                // Formato muy viejo: "itemCount:id-x-y-z-rot-state;...:state:dir:pos:delay"
                ParseLegacyFullString(wiredData);
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        private void ParseLegacyItemsString(string itemsStr)
        {
            if (string.IsNullOrEmpty(itemsStr)) return;

            foreach (var entry in itemsStr.Split(';'))
            {
                if (string.IsNullOrEmpty(entry)) continue;

                // formato: "itemId:x,y,z,rotation,extraData"
                var colonIdx = entry.IndexOf(':');
                if (colonIdx < 0) continue;

                if (!int.TryParse(entry.Substring(0, colonIdx), out int id)) continue;

                var item = Instance.GetRoomItemHandler().GetItem(id);
                if (item == null) continue;

                var coords = entry.Substring(colonIdx + 1).Split(',');

                var setting = new FurniSetting { itemId = id };

                // Variables temporales para evitar usar propiedades como out
                int xTemp = 0, yTemp = 0, rotationTemp = 0;
                double zTemp = 0.0;

                if (coords.Length > 0) int.TryParse(coords[0], out xTemp);
                if (coords.Length > 1) int.TryParse(coords[1], out yTemp);
                if (coords.Length > 2) double.TryParse(coords[2],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out zTemp);
                if (coords.Length > 3) int.TryParse(coords[3], out rotationTemp);

                setting.x = xTemp;
                setting.y = yTemp;
                setting.z = zTemp;
                setting.rotation = rotationTemp;
                setting.extraData = coords.Length > 4 ? coords[4] : "";

                SetItems.TryAdd(item.Id, item);
                _settings.Add(setting);
            }
        }

        private void ParseLegacyFullString(string wiredData)
        {
            var parts = wiredData.Split(':');
            if (parts.Length < 6) return;

            ParseLegacyItemsString(parts[1]);

            _state = parts[2] == "1";
            _direction = parts[3] == "1";
            _position = parts[4] == "1";
            _altitude = false;
            if (int.TryParse(parts[5], out int delay)) Delay = delay;

            _furniSource = _settings.Count == 0
                ? WiredSourceUtil.SOURCE_TRIGGER
                : WiredSourceUtil.SOURCE_SELECTED;
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