using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Wired
{
    public class WiredRoomSettingsSaveEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            Room room = session.GetHabbo()?.CurrentRoom;
            if (room == null) return;

            // Necesitamos al menos 2 ints (8 bytes)
            if (packet.RemainingLength < 8)
            {
                session.SendMessage(new WiredRoomSettingsDataComposer(room, session.GetHabbo()));
                return;
            }

            if (!room.canManageWiredSettings(session.GetHabbo()))
            {
                session.SendMessage(new WiredRoomSettingsDataComposer(room, session.GetHabbo()));
                return;
            }

            int inspectMask = packet.PopInt();
            int modifyMask  = packet.PopInt();

            room.SaveWiredSettings(inspectMask, modifyMask);

            session.SendMessage(new WiredRoomSettingsDataComposer(room, session.GetHabbo()));
        }
    }
}
