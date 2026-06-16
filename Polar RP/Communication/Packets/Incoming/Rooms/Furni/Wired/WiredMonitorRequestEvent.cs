using Polar.Communication.Packets.Outgoing.Rooms.Furni.Wired;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Rooms.Furni.Wired
{
    class WiredMonitorRequestEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (session?.GetHabbo()?.CurrentRoom == null) return;
            session.SendMessage(new WiredMonitorDataComposer(session.GetHabbo().CurrentRoom.WiredVariables));
        }
    }
}