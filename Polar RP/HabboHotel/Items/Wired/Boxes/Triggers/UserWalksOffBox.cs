using System;
using System.Collections.Concurrent;
using Polar.HabboHotel.Rooms.Instance;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class UserWalksOffBox : IWiredItem, IWiredCustomData
    {
        

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerWalkOffFurni;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int furniSource = WiredSourceUtil.SOURCE_TRIGGER;

        public UserWalksOffBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            StringData = "";
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            packet.PopInt();
            int fSource = packet.PopInt();
            packet.PopString();

            this.SetItems.Clear();
            this.furniSource = NormalizeFurniSource(fSource);

            int furniCount = packet.PopInt();
            for (int i = 0; i < furniCount; i++)
            {
                int itemId = packet.PopInt();
                Item selected = Instance.GetRoomItemHandler().GetItem(itemId);
                if (selected != null)
                    SetItems.TryAdd(selected.Id, selected);
            }

            // Al final del HandleSave, después del loop
            if (SetItems.Count > 0)
                this.furniSource = WiredSourceUtil.SOURCE_SELECTED;
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                furniSource = this.furniSource,
                itemIds = SetItems.Keys.ToList()
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.SetItems.Clear();
            this.furniSource = WiredSourceUtil.SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.furniSource = NormalizeFurniSource(data.furniSource);

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        SetItems.TryAdd(item.Id, item);
                }

                // Si hay items cargados, forzar SOURCE_SELECTED
                if (SetItems.Count > 0)
                    this.furniSource = WiredSourceUtil.SOURCE_SELECTED;
            }
            else
            {
                // Retrocompatibilidad: formato viejo "delay:?:id1;id2;"
                var parts = wiredData.Split(':');
                if (parts.Length >= 3 && parts[2] != "\t")
                {
                    foreach (var s in parts[2].Split(';'))
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        if (!int.TryParse(s, out int id)) continue;

                        var item = Instance.GetRoomItemHandler().GetItem(id);
                        if (item != null)
                            SetItems.TryAdd(item.Id, item);
                    }
                }

                furniSource = SetItems.Count == 0 ? WiredSourceUtil.SOURCE_TRIGGER : WiredSourceUtil.SOURCE_SELECTED;
            }

            this.ItemsData = string.Join(";", SetItems.Keys);
        }

        public void Serialize(ServerPacket packet)
        {
            var toRemove = SetItems.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                SetItems.TryRemove(id, out _);

            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(SetItems.Count);
            foreach (var id in SetItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(1);
            packet.WriteInteger(this.furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo Player = Params.Length > 0 ? Params[0] as Habbo : null;
            Item source = Params.Length > 1 ? Params[1] as Item : null;

            if (Player == null) return false;

            if (furniSource == WiredSourceUtil.SOURCE_SELECTED && (source == null || !SetItems.ContainsKey(source.Id)))
                return false;

            var Effects = Instance.GetWired().GetEffects(this);
            var Conditions = Instance.GetWired().GetConditions(this);
            var addons = Instance.GetWired().GetTriggers(this)
                                     .Where(x => x.Type.ToString().StartsWith("Addon")).ToList();

            var limitAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonExecutionLimit);
            if (limitAddon != null && !limitAddon.Execute()) return false;

            var randomAddon = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandom);
            if (randomAddon != null && !randomAddon.Execute()) return false;

            bool hasOrEval = addons.Any(x => x.Type == WiredBoxType.AddonOrEval);
            if (hasOrEval)
            {
                if (Conditions.Count > 0 && !Conditions.Any(c => c.Execute(Player))) return false;
            }
            else
            {
                foreach (var Condition in Conditions.ToList())
                {
                    if (!Condition.Execute(Player)) return false;
                    Instance.GetWired().OnEvent(Condition.Item);
                }
            }

            bool hasExecuteInOrder = addons.Any(x => x.Type == WiredBoxType.AddonExecuteInOrder);
            bool hasRandomEffect = addons.Any(x => x.Type == WiredBoxType.AddonRandomEffect);
            bool hasUnseen = addons.Any(x => x.Type == WiredBoxType.AddonUnseen);

            if (hasRandomEffect)
            {
                var RandomBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonRandomEffect);
                if (RandomBox == null || !RandomBox.Execute()) return false;

                var SelectedBox = Instance.GetWired().GetRandomEffect(Effects.ToList());
                if (SelectedBox != null && SelectedBox.Execute(Player))
                    Instance.GetWired().OnEvent(SelectedBox.Item);

                Instance.GetWired().OnEvent(RandomBox.Item);
            }
            else if (hasUnseen)
            {
                var unseenBox = addons.FirstOrDefault(x => x.Type == WiredBoxType.AddonUnseen);
                if (unseenBox != null && unseenBox.Execute(Effects.ToList(), Player))
                    Instance.GetWired().OnEvent(unseenBox.Item);
            }
            else if (hasExecuteInOrder)
            {
                foreach (var Effect in Effects.OrderBy(x => x.Item.GetZ).ToList())
                {
                    if (!Effect.Execute(Player)) break;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }
            else
            {
                foreach (var Effect in Effects.ToList())
                {
                    if (!Effect.Execute(Player)) continue;
                    Instance.GetWired().OnEvent(Effect.Item);
                }
            }

            return true;
        }

        private int NormalizeFurniSource(int value)
        {
            if (value == WiredSourceUtil.SOURCE_SELECTED || value == WiredSourceUtil.SOURCE_SELECTOR)
                return value;
            return WiredSourceUtil.SOURCE_TRIGGER;
        }

        private class JsonData
        {
            public int furniSource { get; set; }
            public List<int> itemIds { get; set; }
        }
    }
}