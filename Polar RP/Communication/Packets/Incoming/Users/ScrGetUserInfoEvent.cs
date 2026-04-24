using System;
using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Users;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class ScrGetUserInfoEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new ScrSendUserInfoComposer(Session.GetHabbo()));
        }
    }
}
