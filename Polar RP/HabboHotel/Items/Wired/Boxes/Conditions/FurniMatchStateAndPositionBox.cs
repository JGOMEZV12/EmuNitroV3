using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    // Clase compartida de snapshot — equivalente a WiredMatchFurniSetting en Java
    public class FurniMatchSetting
    {
        public int itemId { get; set; }
        public string state { get; set; }
        public int rotation { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public double z { get; set; }
    }

    public class FurniMatchStateAndPositionBox : IWiredItem, IWiredCustomData
    {
        public const int QUANTIFIER_ALL = 0;
        public const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionMatchStateAndPosition;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private bool state = false;
        private bool position = false;
        private bool direction = false;
        private bool altitude = false;
        public int furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        public int quantifier = QUANTIFIER_ALL;
        public List<FurniMatchSetting> settings = new();

        public FurniMatchStateAndPositionBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();
            settings.Clear();

            // 1. Int params
            int paramsCount = packet.PopInt();
            bool rawState = paramsCount > 0 && packet.PopInt() == 1;
            bool rawDir = paramsCount > 1 && packet.PopInt() == 1;
            bool rawPos = paramsCount > 2 && packet.PopInt() == 1;
            bool rawAlt = paramsCount > 3 && packet.PopInt() == 1;
            int rawSource = paramsCount > 4 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawQuant = paramsCount > 5 ? packet.PopInt() : QUANTIFIER_ALL;

            // 2. String param
            string strParam = packet.PopString();

            // 3. Furnis (sin delay)
            int furniCount = packet.PopInt();
            Console.WriteLine($"[FurniMatchStateAndPositionBox] state={rawState}, dir={rawDir}, pos={rawPos}, alt={rawAlt}, source={rawSource}, quant={rawQuant}, furniCount={furniCount}");

            for (int i = 0; i < furniCount; i++)
            {
                int rawId = packet.PopInt();
                Item selected = Instance.GetRoomItemHandler().GetItem(rawId);
                Console.WriteLine($"  furni[{i}] id={rawId}, found={selected != null}");
                if (selected == null) continue;

                SetItems.TryAdd(selected.Id, selected);
                settings.Add(new FurniMatchSetting
                {
                    itemId = selected.Id,
                    state = selected.ExtraData ?? "",
                    rotation = selected.Rotation,
                    x = selected.GetX,
                    y = selected.GetY,
                    z = selected.GetZ
                });
            }

            if (SetItems.Count > 0 && rawSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                rawSource = WiredBoxTypeUtility.SOURCE_SELECTED;

            this.state = rawState;
            this.direction = rawDir;
            this.position = rawPos;
            this.altitude = rawAlt;
            this.furniSource = rawSource;
            this.quantifier = rawQuant == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

            this.StringData = $"{(state ? 1 : 0)};{(direction ? 1 : 0)};{(position ? 1 : 0)}";
            this.ItemsData = string.Join(";", settings.Select(s =>
                $"{s.itemId}:{s.x},{s.y},{s.z},{s.rotation},{s.state}"));
        }

        public string GetWiredData()
        {
            Refresh();
            return JsonConvert.SerializeObject(new JsonData
            {
                state = this.state,
                position = this.position,
                direction = this.direction,
                altitude = this.altitude,
                settings = this.settings,
                furniSource = this.furniSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            settings.Clear();
            this.state = false;
            this.position = false;
            this.direction = false;
            this.altitude = false;
            this.furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ALL;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.state = data.state;
                this.position = data.position;
                this.direction = data.direction;
                this.altitude = data.altitude;
                this.furniSource = data.furniSource;
                this.quantifier = data.quantifier == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

                if (data.settings != null)
                {
                    foreach (var s in data.settings)
                    {
                        var item = Instance.GetRoomItemHandler().GetItem(s.itemId);
                        if (item == null) continue;

                        SetItems.TryAdd(item.Id, item);
                        settings.Add(s);
                    }
                }
            }
            else
            {
                // Retrocompatibilidad: "count:id-state-rot-x-y-z;...:state:dir:pos"
                var parts = wiredData.Split(':');
                if (parts.Length >= 5)
                {
                    int count = int.TryParse(parts[0], out int c) ? c : 0;
                    var itemParts = parts[1].Split(';');

                    for (int i = 0; i < count && i < itemParts.Length; i++)
                    {
                        var stuff = itemParts[i].Split('-');
                        if (stuff.Length < 5) continue;

                        int itemId = int.Parse(stuff[0]);
                        var item = Instance.GetRoomItemHandler().GetItem(itemId);
                        if (item == null) continue;

                        SetItems.TryAdd(item.Id, item);
                        settings.Add(new FurniMatchSetting
                        {
                            itemId = itemId,
                            state = stuff[1],
                            rotation = int.Parse(stuff[2]),
                            x = int.Parse(stuff[3]),
                            y = int.Parse(stuff[4]),
                            z = stuff.Length >= 6 ? double.Parse(stuff[5]) : 0
                        });
                    }

                    this.state = parts[2] == "1";
                    this.direction = parts[3] == "1";
                    this.position = parts[4] == "1";
                    this.altitude = false;
                    this.furniSource = settings.Count == 0
                        ? WiredBoxTypeUtility.SOURCE_TRIGGER
                        : WiredBoxTypeUtility.SOURCE_SELECTED;
                    this.quantifier = QUANTIFIER_ALL;
                }
            }

            this.StringData = $"{(state ? 1 : 0)};{(direction ? 1 : 0)};{(position ? 1 : 0)}";
            this.ItemsData = string.Join(";", settings.Select(s =>
                $"{s.itemId}:{s.x},{s.y},{s.z},{s.rotation},{s.state}"));
        }

        public void Serialize(ServerPacket packet)
        {
            Refresh();

            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(settings.Count);
            foreach (var s in settings)
                packet.WriteInteger(s.itemId);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(6);
            packet.WriteInteger(state ? 1 : 0);
            packet.WriteInteger(direction ? 1 : 0);
            packet.WriteInteger(position ? 1 : 0);
            packet.WriteInteger(altitude ? 1 : 0);
            packet.WriteInteger(furniSource);
            packet.WriteInteger(quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            Refresh();
            if (settings.Count == 0) return true;

            return quantifier == QUANTIFIER_ANY
                ? EvaluateAny()
                : EvaluateAll();
        }

        protected bool EvaluateAll()
        {
            foreach (var s in settings)
            {
                var item = Instance.GetRoomItemHandler().GetItem(s.itemId);
                if (item == null) continue;
                if (!MatchesSetting(item, s)) return false;
            }
            return true;
        }

        protected bool EvaluateAny()
        {
            foreach (var s in settings)
            {
                var item = Instance.GetRoomItemHandler().GetItem(s.itemId);
                if (item == null) continue;
                if (MatchesSetting(item, s)) return true;
            }
            return false;
        }

        protected bool MatchesSetting(Item item, FurniMatchSetting s)
        {
            if (state && item.ExtraData != s.state) return false;
            if (position && (item.GetX != s.x || item.GetY != s.y)) return false;
            if (altitude && Math.Abs(item.GetZ - s.z) > 0.001) return false;
            if (direction && item.Rotation != s.rotation) return false;
            return true;
        }

        public void Refresh()
        {
            settings.RemoveAll(s => Instance.GetRoomItemHandler().GetItem(s.itemId) == null);
            var toRemove = SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                SetItems.TryRemove(id, out _);
        }

        private class JsonData
        {
            public bool state { get; set; }
            public bool position { get; set; }
            public bool direction { get; set; }
            public bool altitude { get; set; }
            public List<FurniMatchSetting> settings { get; set; }
            public int furniSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}