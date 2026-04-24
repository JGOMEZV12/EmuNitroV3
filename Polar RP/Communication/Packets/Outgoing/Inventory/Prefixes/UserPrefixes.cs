using System.Collections.Generic;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing;

namespace Polar.Communication.Packets.Outgoing.Inventory.Prefixes
{
    public class UserPrefixesComposer : ServerPacket
    {
        public UserPrefixesComposer(Habbo habbo)
            : base(ServerPacketHeader.UserPrefixesComposer)
        {
            if (habbo == null) return;

            List<UserPrefix> prefixes = habbo.GetInventoryComponent()
                .GetPrefixesComponent().GetPrefixes();

            WriteInteger(prefixes.Count);
            foreach (UserPrefix prefix in prefixes)
            {
                WriteInteger(prefix.GetId());
                WriteString(prefix.GetText());
                WriteString(prefix.GetColor());
                WriteString(prefix.GetIcon());
                WriteString(prefix.GetEffect());
                WriteInteger(prefix.IsActive() ? 1 : 0);
            }
        }
    }
}