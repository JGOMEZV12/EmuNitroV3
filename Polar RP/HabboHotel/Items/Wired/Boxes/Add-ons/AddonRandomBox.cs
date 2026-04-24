using Polar.Communication.Packets.Outgoing;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Rooms;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonRandomBox : IWiredItem
    {
        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonRandom;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private const int DEFAULT_PICK_AMOUNT = 1;
        private const int DEFAULT_SKIP_EXECUTIONS = 0;
        private const int MAX_PICK_AMOUNT = 1000;
        private const int MAX_SKIP_EXECUTIONS = 1000;

        private readonly object _lock = new object();
        private readonly LinkedList<List<int>> _recentExecutionEffectIds = new LinkedList<List<int>>();

        private int _pickAmount = DEFAULT_PICK_AMOUNT;
        private int _skipExecutions = DEFAULT_SKIP_EXECUTIONS;

        public int PickAmount => _pickAmount;
        public int SkipExecutions => _skipExecutions;

        public AddonRandomBox(Room instance, Item item)
        {
            this.Instance = instance;
            this.Item = item;
            this.SetItems = new ConcurrentDictionary<int, Item>();
            this.StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int resolvedPickAmount = packet.PopInt();
            int resolvedSkipExecutions = packet.PopInt();

            _pickAmount = NormalizePickAmount(resolvedPickAmount);
            _skipExecutions = NormalizeSkipExecutions(resolvedSkipExecutions);
            ClearRecentExecutions();

            StringData = JsonSerializer.Serialize(new JsonData(_pickAmount, _skipExecutions));
        }

        public void Serialize(ServerPacket packet)
        {
            packet.WriteBoolean(false);
            packet.WriteInteger(0);
            packet.WriteInteger(0);
            packet.WriteInteger(Item.GetBaseItem().SpriteId);
            packet.WriteInteger(Item.Id);
            packet.WriteString("");
            packet.WriteInteger(2);
            packet.WriteInteger(_pickAmount);
            packet.WriteInteger(_skipExecutions);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public void LoadData(string wiredData)
        {
            OnPickUp();

            if (string.IsNullOrEmpty(wiredData))
                return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonSerializer.Deserialize<JsonData>(wiredData);
                _pickAmount = NormalizePickAmount(data?.PickAmount ?? DEFAULT_PICK_AMOUNT);
                _skipExecutions = NormalizeSkipExecutions(data?.SkipExecutions ?? DEFAULT_SKIP_EXECUTIONS);
            }
        }

        public void OnPickUp()
        {
            _pickAmount = DEFAULT_PICK_AMOUNT;
            _skipExecutions = DEFAULT_SKIP_EXECUTIONS;
            ClearRecentExecutions();
        }

        public bool Execute(params object[] @params)
        {
            return false;
        }

        /// <summary>
        /// Selects random effects from the list, respecting pickAmount and skipExecutions.
        /// </summary>
        public List<T> SelectRandomEffects<T>(List<T> effects, Func<T, int> idResolver)
        {
            lock (_lock)
            {
                if (effects == null || effects.Count == 0)
                    return new List<T>();

                var shuffled = effects.OrderBy(_ => Random.Shared.Next()).ToList();
                int desiredAmount = Math.Min(_pickAmount, shuffled.Count);
                var recentIds = GetRecentEffectIds();
                var selected = new LinkedList<T>();
                var selectedSet = new HashSet<T>();

                // First pass: skip recently used effects
                foreach (var effect in shuffled)
                {
                    if (recentIds.Contains(idResolver(effect)))
                        continue;

                    if (selectedSet.Add(effect))
                        selected.AddLast(effect);

                    if (selected.Count >= desiredAmount)
                        break;
                }

                // Second pass: fill remaining slots if needed
                if (selected.Count < desiredAmount)
                {
                    foreach (var effect in shuffled)
                    {
                        if (selectedSet.Add(effect))
                            selected.AddLast(effect);

                        if (selected.Count >= desiredAmount)
                            break;
                    }
                }

                RecordExecution(selected, idResolver);
                return selected.ToList();
            }
        }

        private void ClearRecentExecutions()
        {
            lock (_lock)
            {
                _recentExecutionEffectIds.Clear();
            }
        }

        private HashSet<int> GetRecentEffectIds()
        {
            var ids = new HashSet<int>();

            if (_skipExecutions <= 0)
                return ids;

            foreach (var executionIds in _recentExecutionEffectIds)
                foreach (var id in executionIds)
                    ids.Add(id);

            return ids;
        }

        private void RecordExecution<T>(IEnumerable<T> selectedEffects, Func<T, int> idResolver)
        {
            if (_skipExecutions <= 0)
            {
                _recentExecutionEffectIds.Clear();
                return;
            }

            var executionIds = new List<int>();
            if (selectedEffects != null)
                foreach (var effect in selectedEffects)
                    if (effect != null)
                        executionIds.Add(idResolver(effect));

            _recentExecutionEffectIds.AddLast(executionIds);

            while (_recentExecutionEffectIds.Count > _skipExecutions)
                _recentExecutionEffectIds.RemoveFirst();
        }

        private static int NormalizePickAmount(int value) =>
            Math.Max(1, Math.Min(MAX_PICK_AMOUNT, value));

        private static int NormalizeSkipExecutions(int value) =>
            Math.Max(0, Math.Min(MAX_SKIP_EXECUTIONS, value));

        private class JsonData
        {
            public int PickAmount { get; set; }
            public int SkipExecutions { get; set; }

            public JsonData() { }
            public JsonData(int pickAmount, int skipExecutions)
            {
                PickAmount = pickAmount;
                SkipExecutions = skipExecutions;
            }
        }
    }
}