using Polar.HabboHotel.Rooms;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    class WiredRoomSettingsDataComposer : ServerPacket
    {
        public WiredRoomSettingsDataComposer(Room room)
            : base(ServerPacketHeader.WiredRoomSettingsDataComposer)
        {
            WriteInteger(room.RoomId);
            WriteBoolean(room.HideWired); // Example setting
            WriteInteger(0); // Extra config
        }
    }
}