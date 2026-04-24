using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Misc;

using Polar.Communication.Packets.Outgoing.Avatar;

namespace Polar.Communication.Packets.Incoming.Avatar
{
    internal class GetWardrobeEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
           /* int ClothingRoom = Convert.ToInt32(RoleplayData.GetData("clothing", "roomid"));

            if (Session.GetRoomUser() == null || !Session.GetHabbo().InRoom)
                Session.SendNotification("Usted debe estar dentro de la tienda de ropa para cambiar su ropa [RoomID: " + ClothingRoom + "]");

            if (Session.GetRoomUser().RoomId != ClothingRoom)
                Session.SendNotification("Usted debe estar dentro de la tienda de ropa para cambiar su ropa [RoomID: " + ClothingRoom + "]");*/

            Session.SendMessage(new WardrobeComposer(Session.GetHabbo().Id));
        }
    }
}
