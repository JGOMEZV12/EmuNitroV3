using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class MoveFurniXYZBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectMoveFurniXYZ;
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
        private int _direction = 0;
        private int _spacing = 1;
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
        private bool _requested;

        // Rastrea el offset actual por ítem (port de indexOffset en Java)
        private readonly Dictionary<int, int> _indexOffset = new Dictionary<int, int>();

        public MoveFurniXYZBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            TickCount = Delay;
            _requested = false;
        }

        // ── IWiredCustomData ───────────────────────────────────────────────────

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                direction = _direction,
                spacing = _spacing,
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _indexOffset.Clear();
            _direction = 0;
            _spacing = 1;
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _direction = data.direction;
                _spacing = data.spacing;
                _furniSource = data.furniSource;
                Delay = data.delay;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var it = Instance.GetRoomItemHandler().GetItem(id);
                    if (it != null)
                        SetItems.TryAdd(it.Id, it);
                }

                if (_furniSource == WiredSourceUtil.SOURCE_TRIGGER && SetItems.Count > 0)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else if (wiredData.Contains('\t'))
            {
                // Retrocompatibilidad Java legacy: "direction\tspacing\tdelay\tid1\rid2"
                var tabs = wiredData.Split('\t');
                if (tabs.Length == 4)
                {
                    int.TryParse(tabs[0], out _direction);
                    int.TryParse(tabs[1], out _spacing);
                    if (int.TryParse(tabs[2], out int delay)) Delay = delay;

                    foreach (var s in tabs[3].Split('\r'))
                    {
                        if (!int.TryParse(s, out int id)) continue;
                        var it = Instance.GetRoomItemHandler().GetItem(id);
                        if (it != null) SetItems.TryAdd(it.Id, it);
                    }
                }

                _furniSource = SetItems.Count == 0
                    ? WiredSourceUtil.SOURCE_TRIGGER
                    : WiredSourceUtil.SOURCE_SELECTED;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();
            _indexOffset.Clear();

            int intCount = packet.PopInt();
            _direction = intCount > 0 ? packet.PopInt() : 0;
            _spacing = intCount > 1 ? packet.PopInt() : 1;
            _furniSource = intCount > 2 ? packet.PopInt() : WiredSourceUtil.SOURCE_TRIGGER;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            packet.PopString(); // string vacío

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                var selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            Delay = packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public void Serialize(ServerPacket packet)
        {
            // Limpiar ítems que ya no están en la sala
            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3); // direction, spacing, furniSource
            packet.WriteInteger(_direction);
            packet.WriteInteger(_spacing);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (SetItems.Count == 0) return false;

            if (!_requested)
            {
                TickCount = Delay;
                _requested = true;
            }
            return true;
        }

        public bool OnCycle()
        {
            if (Instance == null || !_requested) return false;

            _requested = false;

            var map = Instance.GetGameMap();
            if (map == null) return false;

            // Limpiar ítems que ya no están en la sala
            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
            {
                SetItems.TryRemove(id, out _);
                _indexOffset.Remove(id);
            }

            var validItems = SetItems.Values
                .Where(i => i != null && Instance.GetRoomItemHandler().GetFloor.Contains(i))
                .ToList();

            if (validItems.Count == 0) return false;

            // Elegir ítem destino (target) aleatoriamente, igual que el Java
            var targetItem = validItems[Random.Shared.Next(validItems.Count)];

            foreach (var movingItem in validItems)
            {
                if (movingItem == null) continue;

                // Calcular offset actual para este ítem
                int offset = 0;
                if (_indexOffset.TryGetValue(targetItem.Id, out int currentOffset))
                    offset = currentOffset + _spacing;

                // Obtener tile de destino en la dirección configurada
                var dest = GetTileInFront(targetItem.GetX, targetItem.GetY, _direction, offset);

                if (dest == null || !map.ValidTile(dest.Value.X, dest.Value.Y))
                {
                    // Reset offset y reintentar desde 0
                    offset = 0;
                    dest = GetTileInFront(targetItem.GetX, targetItem.GetY, _direction, offset);
                }

                if (dest == null) continue;

                // Verificar que el tile permite apilado y movimiento
                if (!map.CanRollItemHere(dest.Value.X, dest.Value.Y)) continue;
                if (map.SquareHasUsers(dest.Value.X, dest.Value.Y)) continue;

                double newZ = map.GetHeightForSquareFromData(dest.Value);

                // Acumular altura de ítems en el destino
                foreach (var coordItem in map.GetCoordinatedItems(dest.Value).ToList())
                {
                    if (coordItem == null || coordItem.Id == movingItem.Id) continue;
                    if (coordItem.TotalHeight > newZ) newZ = coordItem.TotalHeight;
                }

                // Mover el ítem
                Instance.GetRoomItemHandler().SetFloorItem(movingItem, dest.Value.X, dest.Value.Y, newZ);
                Instance.GetWired().OnUserFurniCollision(Instance, movingItem);

                _indexOffset[targetItem.Id] = offset;
            }

            return true;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Port de RoomLayout.getTileInFront: devuelve el tile a N pasos
        /// en la dirección indicada (0=N, 2=E, 4=S, 6=W).
        /// </summary>
        private Point? GetTileInFront(int x, int y, int direction, int steps)
        {
            if (steps < 0) steps = 0;

            int dx = 0, dy = 0;
            switch (direction)
            {
                case 0: dy = -1; break; // Norte
                case 2: dx = 1; break; // Este
                case 4: dy = 1; break; // Sur
                case 6: dx = -1; break; // Oeste
                default: return new Point(x, y);
            }

            var map = Instance.GetGameMap();
            int nx = x + dx * (steps + 1);
            int ny = y + dy * (steps + 1);

            if (!map.ValidTile(nx, ny)) return null;
            return new Point(nx, ny);
        }

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public int direction { get; set; }
            public int spacing { get; set; }
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
        }
    }
}