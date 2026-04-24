using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Catalog;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.Communication.Packets.Outgoing.BuildersClub;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal class GetCatalogModeEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            string mode = Packet.PopString();
            //Session.SendMessage(new CatalogIndexComposer(Session, PolarEnvironment.GetGame().GetCatalog().GetPages()));
        }
    }
}
