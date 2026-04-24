using Polar.Communication.Packets.Incoming;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets
{
    public interface IPacketEvent
    {
        void Parse(GameClient Session, ClientPacket Packet);
    }
}