using System.Collections.Concurrent;

namespace Polar.HabboHotel.Rooms
{
    public class RoomVariableManager
    {
        private readonly Room _room;
        private readonly ConcurrentDictionary<string, int> _variables;

        public RoomVariableManager(Room room)
        {
            _room = room;
            _variables = new ConcurrentDictionary<string, int>();
        }

        public int? GetAssignment(string name)
        {
            if (_variables.TryGetValue(name, out int value)) return value;
            return null;
        }

        public void SetAssignment(string name, int value)
        {
            _variables[name] = value;
        }
    }
}
