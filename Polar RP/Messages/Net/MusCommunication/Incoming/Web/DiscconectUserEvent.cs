using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Users;
using Polar.Messages.Net.MusCommunication.Outgoing.Web;
using Polar.Net;
using System;

namespace Polar.Messages.Net.MusCommunication.Incoming.Web
{
    class DiscconectUserEvent : IMusPacketEvent
    {
        public Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            // Desconectamos al usuario
            int UserID = 0;
            if (!int.TryParse(Packet.PacketData, out UserID))
                return Task.CompletedTask;

            Habbo Habbo = PolarEnvironment.GetHabboById(UserID);
            if (Habbo == null)
                return Task.CompletedTask;

            if (Habbo.GetClient() == null)
                return Task.CompletedTask;

            Habbo.GetClient().Disconnect(false);

            return Task.CompletedTask;
        }
    }
}
