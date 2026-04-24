using Polar.Core;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminCreatePageEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            string caption     = packet.PopString();
            string caption2    = packet.PopString();
            string layout      = packet.PopString();
            int    iconType    = packet.PopInt();
            int    minRank     = packet.PopInt();
            bool   visible     = packet.PopBoolean();
            bool   enabled     = packet.PopBoolean();
            int    orderNum    = packet.PopInt();
            int    parentId    = packet.PopInt();
            string pageTypeStr = packet.PopString();
            string catalogMode = packet.PopString();

            CatalogPage page = PolarEnvironment.GetGame().GetCatalog()
                .CreateCatalogPage(caption, caption2, 0, iconType, layout, minRank, parentId, pageTypeStr, catalogMode);

            if (page == null)
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "Failed to create page"));
                return;
            }

            session.SendMessage(new CatalogAdminResultComposer(true, $"Page created: {page.Id}"));
        }
    }
}
