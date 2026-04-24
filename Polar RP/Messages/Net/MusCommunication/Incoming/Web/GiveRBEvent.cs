using Polar.Messages.Net.MusCommunication.Outgoing.Web;
using Polar.Net;
using System;
using Polar.HabboHotel.Users;
using Polar.Communication.Packets.Outgoing.Inventory.Purse;

namespace Polar.Messages.Net.MusCommunication.Incoming.Web
{
    class GiveRBEvent : IMusPacketEvent
    {
        public Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            string[] D = Packet.PacketData.Split('|');

            int Amount = 0, UserID = 0;

            if (!int.TryParse(D[0], out UserID))
                return Task.CompletedTask;

            if (!int.TryParse(D[1], out Amount))
                return Task.CompletedTask;

            Habbo Habbo = PolarEnvironment.GetHabboById(UserID);
            if (Habbo == null)
                return Task.CompletedTask;

            if (Habbo.GetClient() == null)
                return Task.CompletedTask;

            Habbo.Diamonds += Amount;
            Habbo.UpdateDiamondsBalance(Amount);

            Habbo.GetClient().SendNotification("¡Has recibido " + Amount.ToString() + " rubies(s)!");

            return Task.CompletedTask;
        }

    }
}
