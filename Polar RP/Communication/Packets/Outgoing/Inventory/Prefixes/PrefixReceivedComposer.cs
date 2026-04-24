using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing;

namespace Polar.Communication.Packets.Outgoing.Inventory.Prefixes
{
    public class PrefixReceivedComposer : ServerPacket
    {
        public PrefixReceivedComposer(UserPrefix prefix)
            : base(ServerPacketHeader.PrefixReceivedComposer)
        {
            WriteInteger(prefix.GetId());
            WriteString(prefix.GetText());
            WriteString(prefix.GetColor());
            WriteString(prefix.GetIcon());
            WriteString(prefix.GetEffect());
        }
    }
}