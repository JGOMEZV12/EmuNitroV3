using System;

using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Handshake;
using Polar.Database.Interfaces;

namespace Polar.Communication.Packets.Incoming.Handshake
{
    public class SSOTicketEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetHabbo() != null)
                return;

            string SSO = Packet.PopString();
            if (string.IsNullOrEmpty(SSO) || SSO.Length < 15)
                return;

            Session.TryAuthenticate(SSO);
            //Session.SendMessage(new SetUniqueIdComposer(Session.GetHabbo().MachineId));
        }
    }
}