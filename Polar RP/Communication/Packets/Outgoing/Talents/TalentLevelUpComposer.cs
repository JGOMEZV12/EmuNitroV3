using Polar.HabboHotel.Achievements;

namespace Polar.Communication.Packets.Outgoing.Talents
{
    internal class TalentLevelUpComposer : ServerPacket
    {
        public Talent talent { get; }
        public TalentLevelUpComposer(Talent Talent)
            : base(ServerPacketHeader.TalentLevelUpMessageComposer)
        {
            this.talent = Talent;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(talent.Type);
            packet.WriteInteger(talent.Level);
            packet.WriteInteger(0);

            if (talent.Type == "citizenship" && talent.Level == 4)
            {
                packet.WriteInteger(2);
                packet.WriteString("HABBO_CLUB_VIP_7_DAYS");
                packet.WriteInteger(7);
                packet.WriteString(talent.Prize);
                packet.WriteInteger(0);
            }
            else
            {
                packet.WriteInteger(1);
                packet.WriteString(talent.Prize);
                packet.WriteInteger(0);
            }
        }
    }
}
