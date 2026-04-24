using System.Collections.Generic;
using Polar.HabboHotel.Achievements;
using Polar.Communication.Packets.Outgoing.Talents;

namespace Polar.Communication.Packets.Incoming.Talents
{
    internal class RetrieveCitizenshipStatus : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string Type = Packet.PopString();

            Session.SendMessage(new TalentTrackLevelComposer(Session, Type));
        }
    }
}
