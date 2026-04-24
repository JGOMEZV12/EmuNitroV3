using Polar.Messages.Net.MusCommunication.Outgoing.Web;
using Polar.Net;
using System;
using Polar.HabboHotel.Users;
using Polar.HabboHotel.GameClients;

namespace Polar.Messages.Net.MusCommunication.Incoming.Web
{
    class GunEvent : IMusPacketEvent
    {
        public Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            string[] D = Packet.PacketData.Split('|');

            int UserID = 0;

            if (!int.TryParse(D[0], out UserID))
                return Task.CompletedTask;

            GameClient Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(UserID);
            if (Client == null || Client.GetHabbo() == null || Client.GetRoleplay() == null)
                return Task.CompletedTask;

            if (Client.GetRoleplay().EquippedWeapon == null)
                return Task.CompletedTask;

            Client.GetRoleplay().EquippedWeapon = null;

            Client.GetRoleplay().OwnedWeapons = null;
            Client.GetRoleplay().OwnedWeapons = Client.GetRoleplay().LoadAndReturnWeapons();

            Client.SendWhisper("El sistema ha actualizado tus armas, puede ser debido a una compra por la web.", 1);
            
            return Task.CompletedTask;
        }
    }
}
