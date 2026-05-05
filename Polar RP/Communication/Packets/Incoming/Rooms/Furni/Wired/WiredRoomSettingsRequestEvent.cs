using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Wired
{
    public class WiredRoomSettingsRequestEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            Room room = session.GetHabbo()?.CurrentRoom;
            if (room == null) return;

            session.SendMessage(new WiredRoomSettingsDataComposer(room, session.GetHabbo()));
        }
    }
}
