// FurniTypeMatchesBox.cs
using System;
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
    class FurniTypeMatchesBox : IWiredItem, IWiredCustomData
    {
        
        protected const int QUANTIFIER_ALL = 0;
        protected const int QUANTIFIER_ANY = 1;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public virtual WiredBoxType Type => WiredBoxType.ConditionFurniTypeMatches;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        // primaryItems — furnis a comparar
        protected ConcurrentDictionary<int, Item> primaryItems = new();
        // secondaryItems — furnis de referencia (tipos a buscar)
        protected ConcurrentDictionary<int, Item> secondaryItems = new();

        protected int furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int compareFurniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
        protected int quantifier = QUANTIFIER_ALL;

        public FurniTypeMatchesBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
        }

        public void HandleSave(ClientPacket packet)
        {
            primaryItems.Clear();
            secondaryItems.Clear();
            SetItems.Clear();

            // 1. Int params
            int paramsCount = packet.PopInt();
            int rawFurniSource = paramsCount > 0 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawCompareSrc = paramsCount > 1 ? packet.PopInt() : WiredBoxTypeUtility.SOURCE_TRIGGER;
            int rawQuantifier = paramsCount > 2 ? packet.PopInt() : QUANTIFIER_ALL;

            // 2. String param — IDs secundarios separados por ";"
            string strParam = packet.PopString();

            // 3. Furnis primarios
            int furniCount = packet.PopInt();
            Console.WriteLine($"[{GetType().Name}] HandleSave — furniSrc={rawFurniSource}, compareSrc={rawCompareSrc}, quant={rawQuantifier}, str='{strParam}', furniCount={furniCount}");

            this.furniSource = NormalizeFurniSource(rawFurniSource);
            this.compareFurniSource = NormalizeFurniSource(rawCompareSrc);
            this.quantifier = rawQuantifier == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

            for (int i = 0; i < furniCount; i++)
            {
                int rawId = packet.PopInt();
                Item selected = Instance.GetRoomItemHandler().GetItem(rawId);
                if (selected == null) continue;
                primaryItems.TryAdd(selected.Id, selected);
                SetItems.TryAdd(selected.Id, selected);
            }

            if (primaryItems.Count > 0 && furniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                furniSource = WiredBoxTypeUtility.SOURCE_SELECTED;

            // Parsear IDs secundarios del string param
            foreach (var part in strParam.Split(new[] { ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(part.Trim(), out int id)) continue;
                var secItem = Instance.GetRoomItemHandler().GetItem(id);
                if (secItem != null)
                    secondaryItems.TryAdd(secItem.Id, secItem);
            }

            if (secondaryItems.Count > 0 && compareFurniSource == WiredBoxTypeUtility.SOURCE_TRIGGER)
                compareFurniSource = WiredBoxTypeUtility.SOURCE_SECONDARY_SELECTED;

            SyncLegacyFields();
        }

        public string GetWiredData()
        {
            Refresh();
            return JsonConvert.SerializeObject(new JsonData
            {
                primaryItemIds = primaryItems.Keys.ToList(),
                secondaryItemIds = secondaryItems.Keys.ToList(),
                furniSource = this.furniSource,
                compareFurniSource = this.compareFurniSource,
                quantifier = this.quantifier
            });
        }

        public void LoadWiredData(string wiredData)
        {
            primaryItems.Clear();
            secondaryItems.Clear();
            SetItems.Clear();
            this.furniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.compareFurniSource = WiredBoxTypeUtility.SOURCE_TRIGGER;
            this.quantifier = QUANTIFIER_ALL;

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                this.furniSource = NormalizeFurniSource(data.furniSource ?? WiredBoxTypeUtility.SOURCE_TRIGGER);
                this.compareFurniSource = NormalizeFurniSource(data.compareFurniSource ??
                    ((data.secondaryItemIds?.Count > 0 || data.itemIds?.Count > 0)
                        ? WiredBoxTypeUtility.SOURCE_SECONDARY_SELECTED
                        : WiredBoxTypeUtility.SOURCE_TRIGGER));
                this.quantifier = (data.quantifier ?? QUANTIFIER_ANY) == QUANTIFIER_ANY ? QUANTIFIER_ANY : QUANTIFIER_ALL;

                LoadItems(data.primaryItemIds ?? new List<int>(), primaryItems);
                LoadItems(data.secondaryItemIds ?? data.itemIds ?? new List<int>(), secondaryItems);
            }
            else
            {
                // Retrocompatibilidad: "id1;id2;id3"
                foreach (var part in wiredData.Split(';'))
                {
                    if (!int.TryParse(part.Trim(), out int id)) continue;
                    var item = Instance.GetRoomItemHandler().GetItem(id);
                    if (item != null)
                        secondaryItems.TryAdd(item.Id, item);
                }

                this.compareFurniSource = secondaryItems.Count == 0
                    ? WiredBoxTypeUtility.SOURCE_TRIGGER
                    : WiredBoxTypeUtility.SOURCE_SECONDARY_SELECTED;
                this.quantifier = QUANTIFIER_ANY;
            }

            foreach (var kvp in primaryItems)
                SetItems.TryAdd(kvp.Key, kvp.Value);

            SyncLegacyFields();
        }

        public void Serialize(ServerPacket packet)
        {
            Refresh();

            packet.WriteBoolean(false);
            packet.WriteInteger(5);
            packet.WriteInteger(primaryItems.Count);
            foreach (var id in primaryItems.Keys)
                packet.WriteInteger(id);

            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            // String param: IDs secundarios
            packet.WriteString(string.Join(";", secondaryItems.Keys));
            packet.WriteInteger(3);
            packet.WriteInteger(this.furniSource);
            packet.WriteInteger(this.compareFurniSource);
            packet.WriteInteger(this.quantifier);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public virtual bool Execute(params object[] Params)
        {
            Refresh();
            return quantifier == QUANTIFIER_ANY
                ? EvaluateAnyMatches(Params)
                : EvaluateAllMatches(Params);
        }

        protected bool EvaluateAllMatches(object[] Params)
        {
            var targets = GetMatchTargets(Params);
            if (targets.Count == 0) return false;

            var compareTypeIds = GetCompareTypeIds(Params);
            if (compareTypeIds.Count == 0) return false;

            return targets.All(item => MatchesType(item, compareTypeIds));
        }

        protected bool EvaluateAnyMatches(object[] Params)
        {
            var targets = GetMatchTargets(Params);
            if (targets.Count == 0) return false;

            var compareTypeIds = GetCompareTypeIds(Params);
            if (compareTypeIds.Count == 0) return false;

            return targets.Any(item => MatchesType(item, compareTypeIds));
        }

        private List<Item> GetMatchTargets(object[] Params)
        {
            if (furniSource == WiredBoxTypeUtility.SOURCE_SELECTED)
                return primaryItems.Values.ToList();

            // SOURCE_TRIGGER: usar el item que disparó
            if (Params.Length > 1 && Params[1] is Item triggered)
                return new List<Item> { triggered };

            return new List<Item>();
        }

        private HashSet<int> GetCompareTypeIds(object[] Params)
        {
            var ids = new HashSet<int>();

            if (compareFurniSource == WiredBoxTypeUtility.SOURCE_SECONDARY_SELECTED)
            {
                foreach (var item in secondaryItems.Values)
                    ids.Add(item.GetBaseItem().Id);
            }
            else if (Params.Length > 1 && Params[1] is Item triggered)
            {
                ids.Add(triggered.GetBaseItem().Id);
            }

            return ids;
        }

        private bool MatchesType(Item item, HashSet<int> compareTypeIds)
            => item != null && compareTypeIds.Contains(item.GetBaseItem().Id);

        protected void Refresh()
        {
            RefreshSelection(primaryItems);
            RefreshSelection(secondaryItems);

            // Sync SetItems con primaryItems
            foreach (var id in SetItems.Keys.Where(id => !primaryItems.ContainsKey(id)).ToList())
                SetItems.TryRemove(id, out _);
        }

        private void RefreshSelection(ConcurrentDictionary<int, Item> selection)
        {
            var toRemove = selection.Keys
                .Where(id => Instance.GetRoomItemHandler().GetItem(id) == null)
                .ToList();
            foreach (var id in toRemove)
                selection.TryRemove(id, out _);
        }

        private void LoadItems(List<int> ids, ConcurrentDictionary<int, Item> target)
        {
            foreach (int id in ids)
            {
                var item = Instance.GetRoomItemHandler().GetItem(id);
                if (item != null)
                    target.TryAdd(item.Id, item);
            }
        }

        protected int NormalizeFurniSource(int value)
        {
            if (value == WiredBoxTypeUtility.SOURCE_TRIGGER ||
                value == WiredBoxTypeUtility.SOURCE_SELECTED ||
                value == WiredBoxTypeUtility.SOURCE_SECONDARY_SELECTED ||
                value == WiredBoxTypeUtility.SOURCE_SELECTOR)
                return value;
            return WiredBoxTypeUtility.SOURCE_TRIGGER;
        }

        private void SyncLegacyFields()
        {
            this.StringData = string.Join(";", secondaryItems.Keys);
            this.ItemsData = string.Join(";", primaryItems.Keys);
        }

        protected class JsonData
        {
            public List<int> primaryItemIds { get; set; }
            public List<int> secondaryItemIds { get; set; }
            public List<int> itemIds { get; set; } // retrocompatibilidad
            public int? furniSource { get; set; }
            public int? compareFurniSource { get; set; }
            public int? quantifier { get; set; }
        }
    }
}