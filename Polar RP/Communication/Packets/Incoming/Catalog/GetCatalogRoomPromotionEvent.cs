using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboRoleplay.Misc;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal class GetCatalogRoomPromotionEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Session.SendMessage(new GetCatalogRoomPromotionComposer(Session.GetHabbo().UsersRooms));

            int OldRoom = Session.GetHabbo().HomeRoom;

            if (OldRoom <= 0)
                OldRoom = 1;

            RoleplayManager.SendUser(Session, OldRoom, "¡Vaya! Al parecer esta Zona ha sido recargada.\n\nIntentaremos enviarte de vuelta.\n\n((Si el problema persiste, puedes reiniciar el client))");

        }
    }
}
