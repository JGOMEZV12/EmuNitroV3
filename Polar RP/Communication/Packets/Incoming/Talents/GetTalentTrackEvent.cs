using System;
using System.Collections.Generic;
using Polar.HabboHotel.Achievements;
using Polar.Communication.Packets.Outgoing.Talents;

namespace Polar.Communication.Packets.Incoming.Talents
{
    internal class GetTalentTrackEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string Type = Packet.PopString();

            List<Talent> talents = PolarEnvironment.GetGame().GetTalentManager().GetTalents(Type, -1);

            if (talents == null)
                return;

            Session.SendMessage(new TalentTrackComposer(Session, Type, talents));
        }
    }
}
