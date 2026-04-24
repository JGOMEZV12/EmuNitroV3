using System;
using System.Linq;
using System.Text;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.Communication.Packets.Incoming.Rooms.Connection
{
    internal class GoToFlatEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session.GetHabbo().InRoom && !Session.GetHabbo().EnterRoom(Session.GetHabbo().CurrentRoom))
            {
                Session.SendMessage(new CloseConnectionComposer());
            }
        }
    }
}
