using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Polar.Communication.Packets.Incoming;
using Polar.Communication.Packets.Outgoing;
using Polar.HabboHotel.Rooms;

namespace Polar.HabboHotel.Items.Wired.Boxes.Add_ons
{
    class AddonExecutionLimitBox : IWiredItem, IWiredCustomData
    {
        public const int MIN_EXECUTIONS = 1;
        public const int MAX_EXECUTIONS = 100;
        public const int DEFAULT_EXECUTIONS = 1;
        public const int MIN_TIME_WINDOW_MS = 1000;
        public const int MAX_TIME_WINDOW_MS = 10000;
        public const int DEFAULT_TIME_MS = 1000;
        public const int TIME_STEP_MS = 500;

        public Room Instance { get; set; }
        public Item Item { get; set; }
        public WiredBoxType Type => WiredBoxType.AddonExecutionLimit;
        public ConcurrentDictionary<int, Item> SetItems { get; set; }
        public string StringData { get; set; }
        public bool BoolData { get; set; }
        public string ItemsData { get; set; }

        private int _maxExecutions = DEFAULT_EXECUTIONS;
        private int _timeWindowMs = DEFAULT_TIME_MS;

        // Timestamps de ejecuciones recientes — equivalente al Deque<Long> del Java
        private readonly LinkedList<long> _recentTimestamps = new();
        private readonly object _lock = new();

        public int MaxExecutions => _maxExecutions;
        public int TimeWindowMs => _timeWindowMs;

        public AddonExecutionLimitBox(Room instance, Item item)
        {
            Instance = instance;
            Item = item;
            SetItems = new ConcurrentDictionary<int, Item>();
            StringData = "";
        }

        public void HandleSave(ClientPacket packet)
        {
            int paramsCount = packet.PopInt();
            int rawExecutions = paramsCount > 0 ? packet.PopInt() : _maxExecutions;
            int rawTimeWindowMs = paramsCount > 1 ? packet.PopInt() : _timeWindowMs;

            string strParam = packet.PopString();

            Console.WriteLine($"[AddonExecutionLimitBox] HandleSave — maxExecutions={rawExecutions}, timeWindowMs={rawTimeWindowMs}");

            _maxExecutions = NormalizeExecutions(rawExecutions);
            _timeWindowMs = NormalizeTimeWindowMs(rawTimeWindowMs);
            ClearRuntimeState();

            StringData = $"{_maxExecutions};{_timeWindowMs}";
        }

        public string GetWiredData()
        {
            return JsonConvert.SerializeObject(new JsonData
            {
                maxExecutions = this._maxExecutions,
                timeWindowMs = this._timeWindowMs
            });
        }

        public void LoadWiredData(string wiredData)
        {
            _maxExecutions = DEFAULT_EXECUTIONS;
            _timeWindowMs = DEFAULT_TIME_MS;
            ClearRuntimeState();

            if (string.IsNullOrEmpty(wiredData)) return;

            if (wiredData.StartsWith("{"))
            {
                var data = JsonConvert.DeserializeObject<JsonData>(wiredData);
                if (data == null) return;

                _maxExecutions = NormalizeExecutions(data.maxExecutions);
                _timeWindowMs = NormalizeTimeWindowMs(data.timeWindowMs);
            }
            else
            {
                // Retrocompatibilidad: "maxExecutions;timeWindowMs"
                var parts = wiredData.Split(';');
                try
                {
                    if (parts.Length > 0) _maxExecutions = NormalizeExecutions(int.Parse(parts[0]));
                    if (parts.Length > 1) _timeWindowMs = NormalizeTimeWindowMs(int.Parse(parts[1]));
                }
                catch
                {
                    _maxExecutions = DEFAULT_EXECUTIONS;
                    _timeWindowMs = DEFAULT_TIME_MS;
                }
            }

            StringData = $"{_maxExecutions};{_timeWindowMs}";
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
            packet.WriteInteger(_maxExecutions);
            packet.WriteInteger(_timeWindowMs);
            packet.WriteInteger(0);
            packet.WriteInteger(WiredBoxTypeUtility.GetWiredId(Type));
            packet.WriteInteger(0);
            packet.WriteInteger(0);
        }

        public bool Execute(params object[] @params)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return TryAcquireExecutionSlot(now);
        }

        // Intenta adquirir un slot — equivalente a tryAcquireExecutionSlot del Java
        public bool TryAcquireExecutionSlot(long timestamp)
        {
            lock (_lock)
            {
                PruneExpiredTimestamps(timestamp);

                if (_recentTimestamps.Count >= _maxExecutions)
                    return false;

                _recentTimestamps.AddLast(timestamp);
                return true;
            }
        }

        public bool CanExecuteAt(long timestamp)
        {
            lock (_lock)
            {
                PruneExpiredTimestamps(timestamp);
                return _recentTimestamps.Count < _maxExecutions;
            }
        }

        public void ClearRuntimeState()
        {
            lock (_lock)
            {
                _recentTimestamps.Clear();
            }
        }

        private void PruneExpiredTimestamps(long timestamp)
        {
            while (_recentTimestamps.Count > 0 &&
                   (timestamp - _recentTimestamps.First.Value) >= _timeWindowMs)
            {
                _recentTimestamps.RemoveFirst();
            }
        }

        private static int NormalizeExecutions(int value) =>
            Math.Max(MIN_EXECUTIONS, Math.Min(MAX_EXECUTIONS, value));

        private static int NormalizeTimeWindowMs(int value)
        {
            if (value < MIN_TIME_WINDOW_MS) return MIN_TIME_WINDOW_MS;
            if (value > MAX_TIME_WINDOW_MS) return MAX_TIME_WINDOW_MS;
            return (int)Math.Round(value / (float)TIME_STEP_MS) * TIME_STEP_MS;
        }

        private class JsonData
        {
            public int maxExecutions { get; set; }
            public int timeWindowMs { get; set; }
        }
    }
}