using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.Users.Effects;

namespace Polar.HabboHotel.Items.Wired.Boxes.Effects
{
    class TeleportUserBox : IWiredItem, IWiredCycle, IWiredCustomData
    {
        private const int MAXIMUM_FURNI_SELECTION = 5;
        private const int TELEPORT_DELAY = 500;

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

        private readonly Queue<RoomUser> _queue;
        private int _delay;
        private bool _fastTeleport = false;
        private int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        private int _userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

        public TeleportUserBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            _queue = new Queue<RoomUser>();
            TickCount = Delay;
        }

        // ── HandleSave — sin cambios, ya funciona ────────────────────────────────
        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();

            bool fastTeleport = false;
            int furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            int userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

            if (paramsCount >= 1) fastTeleport = packet.PopInt() == 1;
            if (paramsCount >= 2) furniSource = packet.PopInt();
            if (paramsCount >= 3) userSource = packet.PopInt();

            packet.PopString();

            SetItems.Clear();
            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            if (SetItems.Count > 0 && furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;

            _fastTeleport = fastTeleport;
            _furniSource = furniSource;
            _userSource = userSource;
            Delay = packet.PopInt();
        }

        // ── IWiredCustomData ─────────────────────────────────────────────────────
        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                delay = this.Delay,
                itemIds = SetItems.Keys.ToList(),
                fastTeleport = this._fastTeleport,
                furniSource = this._furniSource,
                userSource = this._userSource
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _fastTeleport = false;
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            _userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                Delay = data.delay;
                _fastTeleport = data.fastTeleport;
                _furniSource = data.furniSource;
                _userSource = data.userSource;

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                if (_furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER && SetItems.Count > 0)
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

                _fastTeleport = false;
                _furniSource = SetItems.Count == 0 ? WiredBoxTypeUtility.SOURCE_TRIGGER : WiredBoxTypeUtility.SOURCE_SELECTED;
                _userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            }

            ItemsData = string.Join(";", SetItems.Keys);
            TickCount = Delay;
        }

        // ── Serialize — sin cambios ──────────────────────────────────────────────
        public void Serialize(ServerPacket packet)
        {
            var itemsList = SetItems.Values.ToList();

            packet.WriteBoolean(false);
            packet.WriteInteger(MAXIMUM_FURNI_SELECTION);
            packet.WriteInteger(itemsList.Count);
            foreach (Item item in itemsList)
                packet.WriteInteger(item.Id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString(StringData ?? string.Empty);

            packet.WriteInteger(3);
            packet.WriteInteger(_fastTeleport ? 1 : 0);
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_userSource);

            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(Delay);
            packet.WriteInteger(0);
        }

        // ── Execute ──────────────────────────────────────────────────────────────
        public bool Execute(params object[] Params)
        {
            if (Params == null || Params.Length == 0) return false;

            Habbo player = Params[0] as Habbo;
            if (player == null) return false;

            RoomUser user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
            if (user == null) return false;

            player.Effects()?.ApplyEffect(EffectsList.Twinkle);
            _queue.Enqueue(user);
            return true;
        }

        // ── OnCycle ──────────────────────────────────────────────────────────────
        public bool OnCycle()
        {
            if (_queue.Count == 0 || SetItems.Count == 0)
            {
                _queue.Clear();
                TickCount = Delay;
                return true;
            }

            while (_queue.Count > 0)
            {
                RoomUser user = _queue.Dequeue();
                if (user == null || user.GetClient()?.GetHabbo()?.CurrentRoom != Instance)
                    continue;

                TeleportUser(user);
            }

            TickCount = Delay;
            return true;
        }

        // ── TeleportUser ─────────────────────────────────────────────────────────
        private void TeleportUser(RoomUser user)
        {
            if (user == null || Instance?.GetGameMap() == null) return;

            var invalidIds = SetItems
                .Where(kv => Instance.GetRoomItemHandler().GetItem(kv.Key) == null)
                .Select(kv => kv.Key)
                .ToList();
            foreach (int id in invalidIds)
                SetItems.TryRemove(id, out _);

            if (SetItems.Count == 0) return;

            var items = SetItems.Values.ToList();
            Item target = items[PolarEnvironment.GetRandomNumber(0, items.Count - 1)];
            if (target == null) return;

            Instance.GetGameMap().TeleportToItem(user, target);
            Instance.GetRoomUserManager().UpdateUserStatusses();
            user.GetClient()?.GetHabbo()?.Effects()?.ApplyEffect(0);
        }

        // ── JsonData ─────────────────────────────────────────────────────────────
        private class JsonData
        {
            public int delay { get; set; }
            public List<int> itemIds { get; set; }
            public bool fastTeleport { get; set; }
            public int furniSource { get; set; }
            public int userSource { get; set; }
        }
    }
}