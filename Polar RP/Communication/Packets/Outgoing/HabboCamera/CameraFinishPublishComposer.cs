using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.Communication.Packets.Outgoing.HabboCamera
{
    internal class CameraFinishPublishComposer : ServerPacket
    {
        public int PicId { get; }
        public CameraFinishPublishComposer(int picId) : base(ServerPacketHeader.CameraFinishPublishMessageComposer)
        {
            this.PicId = picId;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteBoolean(true);
            packet.WriteInteger(1);
            packet.WriteString(PicId.ToString());
        }
    }
}
