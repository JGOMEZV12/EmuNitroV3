using Polar.HabboHotel.GameClients;
using Polar.Messages.Net.MusCommunication.Outgoing.Phones;
using Polar.Net;
using System;
using System.Threading.Tasks;

namespace Polar.Messages.Net.MusCommunication.Incoming.Phones
{
    class ShowPhoneErrorEvent : IMusPacketEvent
    {
        public async Task Parse(MusConnection MUS, MusPacketEvent Packet)
        {
            string[] D = Packet.PacketData.Split('|');

            GameClient Client = null;
            if (PolarEnvironment.GetGame() != null && PolarEnvironment.GetGame().GetClientManager() != null)
                Client = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Convert.ToInt32(D[1]));

            if (Client != null)
            {
                if (Client.GetRoleplay().Phone <= 0)
                    return;

                if (!Client.GetRoleplay().OwnedPhonesApps.ContainsKey(Convert.ToInt32(D[0])))
                    return;

                PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error," + D[2] + "|");
            }
        }
    }
}
