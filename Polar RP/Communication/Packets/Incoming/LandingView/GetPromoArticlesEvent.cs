using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.LandingView;
using Polar.HabboHotel.LandingView.Promotions;
using Polar.Communication.Packets.Outgoing.LandingView;
using Polar.Communication.Packets.Outgoing.Rooms.Session;

namespace Polar.Communication.Packets.Incoming.LandingView
{
    internal class GetPromoArticlesEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            //ICollection<Promotion> LandingPromotions = PolarEnvironment.GetGame().GetLandingManager().GetPromotionItems();

            //Session.SendMessage(new RoomForwardComposer(Session.GetHabbo().HomeRoom == 0 ? 1 : Session.GetHabbo().HomeRoom));
        }
    }
}
