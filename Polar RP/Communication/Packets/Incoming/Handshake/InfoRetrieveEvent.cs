using System;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.Groups;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Handshake;

namespace Polar.Communication.Packets.Incoming.Handshake
{
    public class InfoRetrieveEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new UserObjectComposer(Session.GetHabbo()));
            Session.SendMessage(new UserPerksComposer(Session.GetHabbo()));
        }
    }
}