using Polar.Core;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Catalog;

namespace Polar.Communication.Packets.Incoming.Catalog.CatalogAdmin
{
    public class CatalogAdminPublishEvent : IPacketEvent
    {
        public void Parse(GameClient session, ClientPacket packet)
        {
            if (!session.GetHabbo().GetPermissions().HasRight("acc_catalogfurni"))
            {
                session.SendMessage(new CatalogAdminResultComposer(false, "No permission"));
                return;
            }

            // Recargar catálogo completo desde DB
            PolarEnvironment.GetGame().GetCatalog().Initialize();

            // Notificar a todos los clientes conectados
            PolarEnvironment.GetGame().GetClientManager().SendMessage(new CatalogUpdatedComposer());

            session.SendMessage(new CatalogAdminResultComposer(true, "Catalog published"));
        }
    }
}
