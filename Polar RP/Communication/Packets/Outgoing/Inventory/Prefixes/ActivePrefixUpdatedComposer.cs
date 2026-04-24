using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing;

namespace Polar.Communication.Packets.Outgoing.Inventory.Prefixes
{
    public class ActivePrefixUpdatedComposer : ServerPacket
    {
        public ActivePrefixUpdatedComposer(UserPrefix prefix)
            : base(ServerPacketHeader.ActivePrefixUpdatedComposer)
        {
            if (prefix != null)
            {
                WriteInteger(prefix.GetId());
                WriteString(prefix.GetText());
                WriteString(prefix.GetColor());
                WriteString(prefix.GetIcon());
                WriteString(prefix.GetEffect());
            }
            else
            {
                WriteInteger(0);
                WriteString("");
                WriteString("");
                WriteString("");
                WriteString("");
            }
        }
    }
}