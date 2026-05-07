using System.Collections.Concurrent;
using System.Data;
using Polar.Database.Interfaces;

namespace Polar.HabboHotel.Rooms
{
    public class RoomUserVariableManager
    {
        private readonly Room _room;
        private readonly ConcurrentDictionary<(int userId, string name), VariableAssignment> _assignments;

        public RoomUserVariableManager(Room room)
        {
            _room = room;
            _assignments = new ConcurrentDictionary<(int, string), VariableAssignment>();
        }

        public VariableAssignment GetAssignment(int userId, string name)
        {
            _assignments.TryGetValue((userId, name), out var assignment);
            return assignment;
        }

        public void SetAssignment(int userId, string name, int value)
        {
            var assignment = _assignments.GetOrAdd((userId, name), k => new VariableAssignment { UserId = userId, Name = name });
            assignment.Value = value;
            Persist(assignment);
        }

        public void RestorePermanentAssignments(int userId)
        {
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT name, value FROM room_user_wired_variables WHERE user_id = @uid AND room_id = @rid");
                dbClient.AddParameter("uid", userId);
                dbClient.AddParameter("rid", _room.Id);
                var table = dbClient.getTable();
                foreach (DataRow row in table.Rows)
                {
                    string name = row["name"].ToString();
                    int value = Convert.ToInt32(row["value"]);
                    _assignments[(userId, name)] = new VariableAssignment { UserId = userId, Name = name, Value = value };
                }
            }
        }

        private void Persist(VariableAssignment assignment)
        {
            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("REPLACE INTO room_user_wired_variables (user_id, room_id, name, value) VALUES (@uid, @rid, @name, @val)");
                dbClient.AddParameter("uid", assignment.UserId);
                dbClient.AddParameter("rid", _room.Id);
                dbClient.AddParameter("name", assignment.Name);
                dbClient.AddParameter("val", assignment.Value);
                dbClient.RunQuery();
            }
        }
    }

    public class VariableAssignment
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
