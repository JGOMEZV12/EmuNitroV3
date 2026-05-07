using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Conditions
{
    internal class TriggererOnFurniBox : IWiredItem, IWiredCustomData
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionTriggererOnFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // En TriggererOnFurniBox:
        protected int _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int _userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int _quantifier = QUANTIFIER_ALL;
        protected const int QUANTIFIER_ALL = 0;
        protected const int QUANTIFIER_ANY = 1;

        protected int Quantifier => _quantifier;

        public TriggererOnFurniBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            SetItems.Clear();

            int intCount = packet.PopInt();
            _furniSource = intCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            _userSource = intCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            _quantifier = intCount > 2 ? NormalizeQuantifier(packet.PopInt()) : QUANTIFIER_ALL;
            for (int i = 3; i < intCount; i++) packet.PopInt();

            packet.PopString();

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                Item selected = Instance.GetRoomItemHandler().GetItem(packet.PopInt());
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                itemIds = SetItems.Keys.ToList(),
                furniSource = _furniSource,
                userSource = _userSource,
                quantifier = _quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            SetItems.Clear();
            _furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            _userSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            _quantifier = QUANTIFIER_ALL;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _furniSource = data.furniSource;
                _userSource = data.userSource;
                _quantifier = NormalizeQuantifier(data.quantifier);

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }
            }
            else
            {
                // Retrocompatibilidad: "id1;id2;id3"
                foreach (var s in wiredData.Split(';'))
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    if (!int.TryParse(s, out int id)) continue;

                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                _furniSource = SetItems.Count == 0
                    ? WiredBoxTypeUtility.SOURCE_TRIGGER
                    : WiredBoxTypeUtility.SOURCE_SELECTED;
            }

            if (SetItems.Count > 0 && _furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                _furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;

            ItemsData = string.Join(";", SetItems.Keys);
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
            packet.WriteInteger(_furniSource);
            packet.WriteInteger(_userSource);
            packet.WriteInteger(_quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            Habbo player = Params.Length > 0 ? Params[0] as Habbo : null;
            if (player == null) return false;

            RoomUser user = player.CurrentRoom?.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
            if (user == null) return false;

            var itemsOnSquare = Instance.GetGameMap().GetAllRoomItemForSquare(user.X, user.Y);

            if (_quantifier == QUANTIFIER_ANY)
                return itemsOnSquare.Any(i => SetItems.ContainsKey(i.Id));

            return SetItems.Keys.All(id => itemsOnSquare.Any(i => i.Id == id));
        }

        private int NormalizeQuantifier(int value) =>
            value == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

        private class JsonData
        {
            public List<int> itemIds { get; set; }
            public int furniSource { get; set; }
            public int userSource { get; set; }
            public int quantifier { get; set; }
        }
    }
}