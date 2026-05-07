using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired;

namespace Polar.HabboHotel.Rooms.Variables
{
    public class RoomVariableManager
    {
        private readonly Room _room;
        private readonly ConcurrentDictionary<int, IWiredItem> _definitions;
        private readonly ConcurrentDictionary<int, string> _values;

        public RoomVariableManager(Room room)
        {
            _room = room;
            _definitions = new ConcurrentDictionary<int, IWiredItem>();
            _values = new ConcurrentDictionary<int, string>();
        }

        public void RegisterDefinition(IWiredItem item)
        {
            _definitions[item.Item.Id] = item;

            // Load value from DB if permanent
            if (item.BoolData)
            {
                LoadValue(item.Item.Id);
            }
        }

        public void UnregisterDefinition(int itemId)
        {
            _definitions.TryRemove(itemId, out _);
            _values.TryRemove(itemId, out _);
        }

        public string GetValue(string variableName)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonRoomVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null)
            {
                // Fallback to the old room variables dictionary
                if (_room.WiredVariables.TryGetValue(variableName, out string val))
                    return val;
                return "0";
            }

            if (_values.TryGetValue(def.Item.Id, out var value))
                return value;

            return "0";
        }

        public void SetValue(string variableName, string value)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonRoomVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null)
            {
                _room.WiredVariables[variableName] = value;
                return;
            }

            _values[def.Item.Id] = value;

            if (def.BoolData)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("REPLACE INTO `room_wired_variables` (`room_id`, `variable_item_id`, `value`) VALUES (@rid, @vid, @val)");
                    dbClient.AddParameter("rid", _room.Id);
                    dbClient.AddParameter("vid", def.Item.Id);
                    dbClient.AddParameter("val", value);
                    dbClient.RunQuery();
                }
            }
        }

        private void LoadValue(int itemId)
        {
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `value` FROM `room_wired_variables` WHERE `room_id` = @rid AND `variable_item_id` = @vid LIMIT 1");
                dbClient.AddParameter("rid", _room.Id);
                dbClient.AddParameter("vid", itemId);
                string val = dbClient.getString();
                if (val != null)
                {
                    _values[itemId] = val;
                }
            }
        }

        public void Cleanup()
        {
            _definitions.Clear();
            _values.Clear();
        }
    }
}
