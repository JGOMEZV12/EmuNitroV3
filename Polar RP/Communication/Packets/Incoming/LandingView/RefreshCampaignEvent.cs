using System;
using Polar.Communication.Packets.Outgoing.LandingView;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.Communication.Packets.Incoming.LandingView
{
    internal class RefreshCampaignEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            //Session.SendMessage(new RoomForwardComposer(Session.GetHabbo().HomeRoom == 0 ? 1 : Session.GetHabbo().HomeRoom));
            try
            {
                String parseCampaings = Packet.PopString();
                if (parseCampaings.Contains("gamesmaker"))
                    return;

                String campaingName = "";
                String[] parser = parseCampaings.Split(';');

                for (int i = 0; i < parser.Length; i++)
                {
                    if (String.IsNullOrEmpty(parser[i]) || parser[i].EndsWith(","))
                        continue;

                    String[] data = parser[i].Split(',');
                    campaingName = data[1];
                }
                Session.SendMessage(new CampaignComposer(parseCampaings, campaingName));
            }
            catch { }
        }
    }
}