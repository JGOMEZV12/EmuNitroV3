using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.HabboCamera
{
    internal class CameraFinishParticipateCompetitionComposer : ServerPacket
    {
        public CameraFinishParticipateCompetitionComposer()
            : base(ServerPacketHeader.CameraFinishParticipateCompetitionMessageComposer)
        {
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(true);
            packet.WriteString("Teste");
        }
    }
}
