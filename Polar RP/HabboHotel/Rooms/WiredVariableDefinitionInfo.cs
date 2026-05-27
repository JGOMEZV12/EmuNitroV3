namespace Polar.HabboHotel.Rooms
{
    public class WiredVariableDefinitionInfo
    {
        public int ItemId { get; }
        public string Name { get; }
        public bool HasValue { get; }
        public int Availability { get; }
        public bool TextConnected { get; }
        public bool ReadOnly { get; }

        public WiredVariableDefinitionInfo(int itemId, string name, bool hasValue, int availability, bool textConnected, bool readOnly)
        {
            ItemId = itemId;
            Name = name;
            HasValue = hasValue;
            Availability = availability;
            TextConnected = textConnected;
            ReadOnly = readOnly;
        }
    }
}