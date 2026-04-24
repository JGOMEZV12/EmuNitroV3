using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Talents
{
    internal class TalentTrackLevelComposer : ServerPacket
    {
        public GameClient Session { get; }
        public string Packet { get; }
        public TalentTrackLevelComposer(GameClient Session, string packet)
            : base(ServerPacketHeader.TalentTrackLevelMessageComposer)
        {
            this.Session = Session;
            this.Packet = packet;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteString(Packet);
            packet.WriteInteger(1);
            packet.WriteInteger(4);
        }
    }
}