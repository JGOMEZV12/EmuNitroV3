
using Polar.HabboHotel.Users;
using Polar.Core;

namespace Polar.Communication.Packets.Outgoing.Handshake
{
    public class UserRightsComposer : ServerPacket
    {
        public UserRightsComposer(Habbo habbo)
            : base(ServerPacketHeader.UserRightsMessageComposer)
        {
            if (habbo.GetClubManager().HasSubscription("habbo_vip"))
                base.WriteInteger(2);
            else
                base.WriteInteger(0);

            base.WriteInteger(habbo.Rank);
            if (habbo.GetPermissions().HasRight("ambassador"))
                base.WriteBoolean(true);
            else
                base.WriteBoolean(false);
        }
    }
}