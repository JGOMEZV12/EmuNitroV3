using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Incoming;
using Polar;

namespace Polar.Communication.Packets.Incoming.Handshake
{
    public class GetClientVersionEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            string Build = Packet.PopString();
            //Out.WriteLine(Build);
            if (PolarEnvironment.SWFRevision != Build)
                PolarEnvironment.SWFRevision = Build;

            
        }
    }
}