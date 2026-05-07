using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired;

namespace Polar.HabboHotel.Rooms.Variables
{
    public class RoomUserVariableManager
    {
        private readonly Room _room;

        // variable_item_id -> Wired Definition
        private readonly ConcurrentDictionary<int, IWiredItem> _definitions;

        // user_id -> variable_item_id -> Value
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> _assignments;

        public RoomUserVariableManager(Room room)
        {
            _room = room;
            _definitions = new ConcurrentDictionary<int, IWiredItem>();
            _assignments = new ConcurrentDictionary<int, ConcurrentDictionary<int, string>>();
        }

        public void RegisterDefinition(IWiredItem item)
        {
            _definitions[item.Item.Id] = item;
        }

        public void UnregisterDefinition(int itemId)
        {
            _definitions.TryRemove(itemId, out _);
            foreach (var userVars in _assignments.Values)
            {
                userVars.TryRemove(itemId, out _);
            }
        }

        public string GetValue(int userId, string variableName)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonUserVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null) return "0";

            if (!_assignments.TryGetValue(userId, out var userVars))
            {
                userVars = LoadUserVariables(userId);
            }

            if (userVars.TryGetValue(def.Item.Id, out var value))
                return value;

            return "0";
        }

        public void SetValue(int userId, string variableName, string value)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonUserVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null) return;

            var userVars = _assignments.GetOrAdd(userId, (id) => LoadUserVariables(id));
            userVars[def.Item.Id] = value;

            // Check if permanent (boolData in UserVariableBox usually means permanent)
            if (def.BoolData)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("REPLACE INTO `room_user_wired_variables` (`room_id`, `user_id`, `variable_item_id`, `value`) VALUES (@rid, @uid, @vid, @val)");
                    dbClient.AddParameter("rid", _room.Id);
                    dbClient.AddParameter("uid", userId);
                    dbClient.AddParameter("vid", def.Item.Id);
                    dbClient.AddParameter("val", value);
                    dbClient.RunQuery();
                }
            }
        }

        private ConcurrentDictionary<int, string> LoadUserVariables(int userId)
        {
            var userVars = new ConcurrentDictionary<int, string>();

            DataTable table;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `variable_item_id`, `value` FROM `room_user_wired_variables` WHERE `room_id` = @rid AND `user_id` = @uid");
                dbClient.AddParameter("rid", _room.Id);
                dbClient.AddParameter("uid", userId);
                table = dbClient.getTable();
            }

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    int vid = Convert.ToInt32(row["variable_item_id"]);
                    userVars.TryAdd(vid, Convert.ToString(row["value"]));
                }
            }

            _assignments.TryAdd(userId, userVars);
            return userVars;
        }

        public void Cleanup()
        {
            _definitions.Clear();
            _assignments.Clear();
        }
    }
}
