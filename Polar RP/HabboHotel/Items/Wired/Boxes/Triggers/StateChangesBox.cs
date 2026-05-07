using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.HabboHotel.Items.Wired.Boxes.Triggers
{
    class StateChangesBox : IWiredItem, IWiredCustomData
    {
        private const int MODE_ALL_STATES = 0;
        private const int MODE_SAVED_STATE = 1;
        private const int SOURCE_TRIGGER = 0;
        private const int SOURCE_SELECTED = 1;
        private const int SOURCE_SELECTOR = 2;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.TriggerStateChanges;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int triggerMode = MODE_ALL_STATES;
        private int furniSource = SOURCE_TRIGGER;

        // itemId → estado guardado del furni al momento de guardar
        private Dictionary<int, string> snapshots = new();

        public StateChangesBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            int intCount = packet.PopInt();
            int tMode = intCount > 0 ? packet.PopInt() : MODE_ALL_STATES;
            packet.PopString(); // string vacío

            int fSource = intCount > 1 ? packet.PopInt() : SOURCE_TRIGGER;

            this.triggerMode = tMode == MODE_SAVED_STATE ? MODE_SAVED_STATE : MODE_ALL_STATES;
            this.furniSource = NormalizeFurniSource(fSource);

            this.SetItems.Clear();
            this.snapshots.Clear();

            int furniCount = packet.PopInt();
            Console.WriteLine($"[StateChangesBox] HandleSave — tMode={tMode}, fSource={fSource}, furniCount={furniCount}");

            for (int i = 0; i < furniCount; i++)
            {
                int itemId = packet.PopInt();
                Item selected = Instance.GetRoomItemHandler().GetItem(itemId);
                Console.WriteLine($"[StateChangesBox] itemId={itemId}, found={selected != null}");
                if (selected == null) continue;

                SetItems.TryAdd(selected.Id, selected);
                snapshots[selected.Id] = NormalizeState(selected.ExtraData);
            }

            if (furniSource == SOURCE_TRIGGER && SetItems.Count > 0)
                furniSource = SOURCE_SELECTED;

            Console.WriteLine($"[StateChangesBox] FINAL — furniSource={furniSource}, SetItems.Count={SetItems.Count}");
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                triggerMode = this.triggerMode,
                furniSource = this.furniSource,
                itemIds = SetItems.Keys.ToList()
            });
        }

        public void LoadWiredData(string wiredData)
        {
            this.SetItems.Clear();
            this.snapshots.Clear();
            this.triggerMode = MODE_ALL_STATES;
            this.furniSource = SOURCE_TRIGGER;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.triggerMode = data.triggerMode;
                this.furniSource = NormalizeFurniSource(data.furniSource);

                foreach (int id in data.itemIds ?? new List<int>())
                {
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item == null) continue;

                    SetItems.TryAdd(item.Id, item);
                    snapshots[item.Id] = NormalizeState(item.ExtraData);
                }

                if (furniSource == SOURCE_TRIGGER && SetItems.Count > 0)
                    furniSource = SOURCE_SELECTED;
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
                        if (item == null) continue;

                        SetItems.TryAdd(item.Id, item);
                        snapshots[item.Id] = NormalizeState(item.ExtraData);
                    }
                }

                furniSource = snapshots.Count == 0 ? SOURCE_TRIGGER : SOURCE_SELECTED;
            }

            this.ItemsData = string.Join(";", SetItems.Keys);
        }

        public void Serialize(ServerPacket packet)
        {
            // Limpiar snapshots de items que ya no están en la sala
            var toRemove = snapshots.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
            {
                snapshots.Remove(id);
                SetItems.TryRemove(id, out _);
            }

            packet.WriteBoolean(false);
            packet.WriteInteger(5); // MAXIMUM_FURNI_SELECTION
            packet.WriteInteger(snapshots.Count);
            foreach (var id in snapshots.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(2);
            packet.WriteInteger(this.triggerMode);
            packet.WriteInteger(this.furniSource);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] Params)
        {
            Habbo Player = Params.Length > 0 ? Params[0] as Habbo : null;
            Item source = Params.Length > 1 ? Params[1] as Item : null;

            if (source == null) return false;

            // Verificar que el furni está en la lista
            if (!SetItems.ContainsKey(source.Id) && furniSource == SOURCE_SELECTED)
                return false;

            // Si modo SAVED_STATE, verificar que el estado actual coincide con el guardado
            if (triggerMode == MODE_SAVED_STATE)
            {
                if (!snapshots.TryGetValue(source.Id, out string savedState))
                    return false;
                if (!NormalizeState(source.ExtraData).Equals(savedState))
                    return false;
            }

            if (Player == null) return false;

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

        private string NormalizeState(string state) => state ?? "";

        private int NormalizeFurniSource(int value)
        {
            if (value == SOURCE_SELECTED || value == SOURCE_SELECTOR)
                return value;
            return SOURCE_TRIGGER;
        }

        private class JsonData
        {
            public int triggerMode { get; set; }
            public int furniSource { get; set; }
            public List<int> itemIds { get; set; } // retrocompatibilidad
        }

        private class StateSnapshot
        {
            public int itemId { get; set; }
            public string state { get; set; }

            public override int GetHashCode() => itemId.GetHashCode();
            public override bool Equals(object obj) =>
                obj is StateSnapshot other && other.itemId == this.itemId;
        }
    }
}