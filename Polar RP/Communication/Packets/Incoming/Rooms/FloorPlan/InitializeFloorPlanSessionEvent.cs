using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.ApartmentsOwned;

namespace Polar.Communication.Packets.Incoming.Rooms.FloorPlan
{
    internal class InitializeFloorPlanSessionEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            if (Session == null || Session.GetRoomUser() == null)
                return;

            ApartmentOwned AP = PolarEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentOwnedById(Session.GetRoomUser().RoomId);

            if (AP != null && !AP.FloorEditor)
            {
                Session.SendNotification("¡Este apartamento no tiene uso de Floor Editor!");
                return;
            }
        }
    }
}
