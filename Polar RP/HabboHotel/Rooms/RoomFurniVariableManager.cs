using System.Collections.Concurrent;

namespace Polar.HabboHotel.Rooms
{
    public class RoomFurniVariableManager
    {
        private readonly Room _room;
        private readonly ConcurrentDictionary<(int itemId, string name), int> _assignments;

        public RoomFurniVariableManager(Room room)
        {
            _room = room;
            _assignments = new ConcurrentDictionary<(int, string), int>();
        }

        public int? GetAssignment(int itemId, string name)
        {
            if (_assignments.TryGetValue((itemId, name), out int value)) return value;
            return null;
        }

        public void SetAssignment(int itemId, string name, int value)
        {
            _assignments[(itemId, name)] = value;
        }
    }
}
