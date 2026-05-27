using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Instance;
using Polar.HabboHotel.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    internal class TeleportUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.EffectTeleportToFurni;
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
        private int _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _userSource = WiredSourceUtil.SOURCE_TRIGGER;
        private int _walkMode = WALKMODE_CONTINUE;
        private bool _requested;
        private Habbo _pendingActor;

        private const int WALKMODE_IF_CLOSER = 0;
        private const int WALKMODE_CONTINUE = 1;
        private const int WALKMODE_STOP = 2;

        private int _lastItemIndex = 0;

        public TeleportUserBox(Room instance, Item item)
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
                delay = Delay,
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource,
                userSource = _userSource,
                walkMode = _walkMode
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _furniSource = WiredSourceUtil.SOURCE_TRIGGER;
            _userSource = WiredSourceUtil.SOURCE_TRIGGER;
            _walkMode = WALKMODE_CONTINUE;
            _lastItemIndex = 0;
            Delay = 0;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Delay = data.delay;
                _furniSource = data.furniSource;
                _userSource = data.userSource;
                _walkMode = NormalizeWalkMode(data.walkMode ?? WALKMODE_CONTINUE);

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var it = Instance.GetRoomItemHandler().GetItem(id);
                    if (it != null) SetItems.TryAdd(it.Id, it);
                }

                if (_furniSource == WiredSourceUtil.SOURCE_TRIGGER && SetItems.Count > 0)
                    _furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else if (wiredData.Contains('\t'))
            {
                // Retrocompatibilidad Java legacy: "delay\tid1;id2;id3"
                var tabs = wiredData.Split('\t');
                if (tabs.Length >= 1 && int.TryParse(tabs[0], out int delay)) Delay = delay;

                if (tabs.Length == 2)
                {
                    foreach (var s in tabs[1].Split(';'))
                    {
                        if (!int.TryParse(s, out int id)) continue;
                        var it = Instance.GetRoomItemHandler().GetItem(id);
                        if (it != null) SetItems.TryAdd(it.Id, it);
                    }
                }

                _furniSource = SetItems.Count == 0
                    ? WiredSourceUtil.SOURCE_TRIGGER
                    : WiredSourceUtil.SOURCE_SELECTED;
                _userSource = WiredSourceUtil.SOURCE_TRIGGER;
                _walkMode = WALKMODE_CONTINUE;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        // ── Packet handling ────────────────────────────────────────────────────

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            // 1. Int params: furniSource, userSource, walkMode
            int intCount = packet.PopInt();
            _furniSource = intCount > 0
                ? packet.PopInt()
                : WiredSourceUtil.SOURCE_TRIGGER;
            _userSource = intCount > 1
                ? packet.PopInt()
                : WiredSourceUtil.SOURCE_TRIGGER;
            _walkMode = intCount > 2
                ? NormalizeWalkMode(packet.PopInt())
                : WALKMODE_CONTINUE;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            // 2. String param (vacío en este wired)
            packet.PopString();

            // 3. Furni seleccionados
            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                var selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            // 4. Delay
            Delay = packet.PopInt();

            // 5. stuffTypeSelectionCode — leer y descartar
            packet.PopInt();

            if (SetItems.Count > 0 && _furniSource == WiredSourceUtil.SOURCE_TRIGGER)
                _furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public void Serialize(ServerPacket packet)
        {
            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(3);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_userSource);
            packet.WriteInteger(_walkMode);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Lógica ─────────────────────────────────────────────────────────────

        public bool Execute(params object[] Params)
        {
            if (SetItems.Count == 0) return false;

            _pendingActor = Params.Length > 0 ? Params[0] as Habbo : null;

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

            foreach (var id in SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList())
                SetItems.TryRemove(id, out _);

            if (SetItems.Count == 0)
            {
                _pendingActor = null;
                return false;
            }

            var validItems = SetItems.Values
                .Where(i => i != null && Instance.GetRoomItemHandler().GetFloor.Contains(i))
                .ToList();

            if (validItems.Count == 0)
            {
                _pendingActor = null;
                return false;
            }

            _lastItemIndex = _lastItemIndex % validItems.Count;
            var targetItem = validItems[_lastItemIndex];
            _lastItemIndex = (_lastItemIndex + 1) % validItems.Count;

            var targets = ResolveTargets();

            foreach (var habbo in targets)
            {
                if (habbo?.GetClient() == null) continue;
                MoveHabboToFurni(habbo, targetItem);
            }

            _pendingActor = null;
            return true;
        }

        // ── Movimiento ────────────────────────────────────────────────────────

        private void MoveHabboToFurni(Habbo habbo, Item targetItem)
        {
            if (habbo == null || targetItem == null) return;

            var user = Instance.GetRoomUserManager().GetRoomUserByHabbo(habbo.Id);
            if (user == null) return;

            var map = Instance.GetGameMap();
            if (map == null) return;

            var destPoint = new Point(targetItem.GetX, targetItem.GetY);
            if (!map.ValidTile(destPoint.X, destPoint.Y)) return;

            var oldPoint = new Point(user.X, user.Y);
            var previousGoal = user.IsWalking ? new Point(user.GoalX, user.GoalY) : (Point?)null;
            bool wasWalking = user.IsWalking;

            if (user.IsWalking) user.ClearMovement(true);

            double newZ = targetItem.TotalHeight;
            user.SetPos(destPoint.X, destPoint.Y, newZ);
            user.UpdateNeeded = true;

            ApplyWalkMode(user, oldPoint, previousGoal, destPoint, wasWalking);

            Instance.GetRoomUserManager().UpdateUserStatusses();
        }

        private void ApplyWalkMode(
            RoomUser user, Point oldLocation, Point? previousGoal,
            Point targetPoint, bool wasWalking)
        {
            if (user == null) return;

            if (_walkMode == WALKMODE_STOP || !wasWalking || previousGoal == null)
            {
                user.MoveTo(targetPoint.X, targetPoint.Y);
                return;
            }

            if (_walkMode == WALKMODE_IF_CLOSER)
            {
                // FIX: operador < correcto (estaba partido en el original)
                bool closer = DistanceSquared(targetPoint, previousGoal.Value) <
                              DistanceSquared(oldLocation, previousGoal.Value);
                if (!closer)
                {
                    user.MoveTo(targetPoint.X, targetPoint.Y);
                    return;
                }
            }

            // WALKMODE_CONTINUE o IF_CLOSER donde sí está más cerca
            user.MoveTo(previousGoal.Value.X, previousGoal.Value.Y);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private List<Habbo> ResolveTargets()
        {
            var result = new List<Habbo>();

            if (_userSource == WiredSourceUtil.SOURCE_TRIGGER ||
                _userSource == WiredSourceUtil.SOURCE_CLICKED_USER)
            {
                if (_pendingActor != null)
                    result.Add(_pendingActor);
            }
            else
            {
                foreach (var ru in Instance.GetRoomUserManager().GetRoomUsers())
                {
                    if (ru == null || ru.IsBot) continue;
                    var h = ru.GetClient()?.GetHabbo();
                    if (h != null) result.Add(h);
                }
            }

            return result;
        }

        private static int DistanceSquared(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static int NormalizeWalkMode(int value) =>
            (value < WALKMODE_IF_CLOSER || value > WALKMODE_STOP)
                ? WALKMODE_CONTINUE
                : value;

        // ── DTO JSON ───────────────────────────────────────────────────────────

        private class JsonData
        {
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
            public int userSource { get; set; }
            public int? walkMode { get; set; }
        }
    }
}