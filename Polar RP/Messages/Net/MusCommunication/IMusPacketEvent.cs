using Polar.HabboHotel.GameClients;
using System.Threading.Tasks;

namespace Polar.Messages.Net.MusCommunication
{
    public interface IMusPacketEvent
    {
        Task Parse(MusConnection MUS, MusPacketEvent Packet);
    }
}