using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Users;

namespace Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired
{
    public class WiredRoomSettingsDataComposer : ServerPacket
    {
        public WiredRoomSettingsDataComposer(Room room, Habbo habbo)
            : base(ServerPacketHeader.WiredRoomSettingsDataComposer)
        {
            int  roomId            = room?.Id ?? 0;
            bool canInspect        = room != null && room.canInspectWired(habbo);
            bool canModify         = room != null && room.canModifyWired(habbo);
            bool canManageSettings = room != null && room.canManageWiredSettings(habbo);
            int  inspectMask       = canInspect ? room.wiredInspectMask : 0;
            int  modifyMask        = canInspect ? room.wiredModifyMask  : 0;

            WriteInteger(roomId);
            WriteInteger(inspectMask);
            WriteInteger(modifyMask);
            WriteBoolean(canInspect);
            WriteBoolean(canModify);
            WriteBoolean(canManageSettings);
        }
    }
}
