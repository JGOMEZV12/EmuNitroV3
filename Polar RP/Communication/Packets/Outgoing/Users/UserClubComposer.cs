using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Users
{
    internal class GetClubComposer : ServerPacket
    {
        public CatalogPage Page { get; }
        public GameClient Session { get; }
        public int Vol { get; }
        public GetClubComposer(CatalogPage page, GameClient session, int Vol) : base(ServerPacketHeader.GetClubComposer)
        {
            this.Session = session;
            this.Page = page;
            this.Vol = Vol;
            Compose(this);
        }

        public void Compose(ServerPacket packet)
        {
            packet.WriteInteger(Page.Items.Values.Count);

            foreach (CatalogItem catalogItem in Page.Items.Values)
            {
                catalogItem.SerializeClub(packet, Session);
            }

            packet.WriteInteger(Vol);
        }

    }

    
}
