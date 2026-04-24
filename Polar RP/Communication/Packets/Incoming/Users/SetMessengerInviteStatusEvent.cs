using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Database.Interfaces;


namespace Polar.Communication.Packets.Incoming.Users
{
    internal class SetMessengerInviteStatusEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Boolean Status = Packet.PopBoolean();

            Session.GetHabbo().AllowMessengerInvites = Status;
            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("UPDATE `users` SET `ignore_invites` = @MessengerInvites WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                dbClient.AddParameter("MessengerInvites", PolarEnvironment.BoolToEnum(Status));
                dbClient.RunQuery();
            }
        }
    }
}
