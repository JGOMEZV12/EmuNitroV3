using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Items.Wired;

namespace Polar.HabboHotel.Rooms.Variables
{
    public class RoomFurniVariableManager
    {
        private readonly Room _room;

        // variable_item_id -> Wired Definition
        private readonly ConcurrentDictionary<int, IWiredItem> _definitions;

        // furni_item_id -> variable_item_id -> Value
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, string>> _assignments;

        public RoomFurniVariableManager(Room room)
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
            foreach (var furniVars in _assignments.Values)
            {
                furniVars.TryRemove(itemId, out _);
            }
        }

        public string GetValue(int furniId, string variableName)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonFurniVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null) return "0";

            if (!_assignments.TryGetValue(furniId, out var furniVars))
            {
                furniVars = LoadFurniVariables(furniId);
            }

            if (furniVars.TryGetValue(def.Item.Id, out var value))
                return value;

            return "0";
        }

        public void SetValue(int furniId, string variableName, string value)
        {
            var def = _definitions.Values.FirstOrDefault(x =>
                x.Type == WiredBoxType.AddonFurniVariable &&
                x.StringData.Equals(variableName, StringComparison.OrdinalIgnoreCase));

            if (def == null) return;

            var furniVars = _assignments.GetOrAdd(furniId, (id) => LoadFurniVariables(id));
            furniVars[def.Item.Id] = value;

            if (def.BoolData)
            {
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("REPLACE INTO `room_furni_wired_variables` (`room_id`, `furni_id`, `variable_item_id`, `value`) VALUES (@rid, @fid, @vid, @val)");
                    dbClient.AddParameter("rid", _room.Id);
                    dbClient.AddParameter("fid", furniId);
                    dbClient.AddParameter("vid", def.Item.Id);
                    dbClient.AddParameter("val", value);
                    dbClient.RunQuery();
                }
            }
        }

        private ConcurrentDictionary<int, string> LoadFurniVariables(int furniId)
        {
            var furniVars = new ConcurrentDictionary<int, string>();

            DataTable table;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `variable_item_id`, `value` FROM `room_furni_wired_variables` WHERE `room_id` = @rid AND `furni_id` = @fid");
                dbClient.AddParameter("rid", _room.Id);
                dbClient.AddParameter("fid", furniId);
                table = dbClient.getTable();
            }

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    int vid = Convert.ToInt32(row["variable_item_id"]);
                    furniVars.TryAdd(vid, Convert.ToString(row["value"]));
                }
            }

            _assignments.TryAdd(furniId, furniVars);
            return furniVars;
        }

        public void Cleanup()
        {
            _definitions.Clear();
            _assignments.Clear();
        }
    }
}
