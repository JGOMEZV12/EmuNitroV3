using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Cache;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Users;

namespace Polar.Communication.Packets.Incoming.Users
{
    internal class GetRelationshipsEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            int UserId = Packet.PopInt();
            
            if (UserId > 1000000)
            {
                int BotId = UserId - 1000000;
                var Bot = RoleplayBotManager.GetCachedBotById(BotId);

                if (Bot != null) { }
                    //Session.SendMessage(new GetRelationshipsComposer(null, Bot));
            }
            else
            {
                UserCache Habbo = PolarEnvironment.GetGame().GetCacheManager().GenerateUser(UserId);

                if (Habbo == null)
                    return;

                //Session.SendMessage(new GetRelationshipsComposer(Habbo));
            }
        }
    }
}
