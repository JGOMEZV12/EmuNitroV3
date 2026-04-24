using Polar.Communication.Packets.Outgoing.HabboCamera;
using Polar.HabboHotel.GameClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Incoming.HabboCamera
{
    internal class ParticipatePictureCameraCompetitionEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {

            Session.SendMessage(new CameraFinishParticipateCompetitionComposer());
        }
    }
}
