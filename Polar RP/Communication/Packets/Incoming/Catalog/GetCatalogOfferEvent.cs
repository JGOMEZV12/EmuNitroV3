using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Catalog;
using Polar.Communication.Packets.Outgoing.Catalog;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Incoming.Catalog
{
    internal class GetCatalogOfferEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            try
            {
                int OfferId = Packet.PopInt();

                ////Console.WriteLine($"GetCatalogOfferEvent: Solicitando oferta {OfferId}");

                var catalog = PolarEnvironment.GetGame().GetCatalog();

                // Método 1: Buscar directamente en las ofertas del manager
                if (catalog.OfferItems.TryGetValue(OfferId, out CatalogItem item))
                {
                    ////Console.WriteLine($"Oferta encontrada en OfferItems: {item.Id}");

                    // Obtener la página del item
                    if (catalog.TryGetPage(item.PageId, out CatalogPage page))
                    {
                        if (!page.CanAccess(Session.GetHabbo().Rank, Session.GetHabbo().VIPRank))
                        {
                            Session.SendNotification("No tienes acceso a esta página");
                            return;
                        }

                        Session.SendMessage(new CatalogOfferComposer(item));
                        //Console.WriteLine($"Oferta enviada: {item.Id}");
                        return;
                    }
                }

                // Método 2: Buscar en todas las páginas
                foreach (var page in catalog.GetPages())
                {
                    if (page.ItemOffers.TryGetValue(OfferId, out item))
                    {
                        //Console.WriteLine($"Oferta encontrada en página {page.Id}: {item.Id}");

                        if (!page.CanAccess(Session.GetHabbo().Rank, Session.GetHabbo().VIPRank))
                        {
                            Session.SendNotification("No tienes acceso a esta página");
                            return;
                        }

                        Session.SendMessage(new CatalogOfferComposer(item));
                        //Console.WriteLine($"Oferta enviada: {item.Id}");
                        return;
                    }
                }

                //Console.WriteLine($"Oferta {OfferId} no encontrada en ningún lado");
                Session.SendNotification("La oferta no está disponible");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error en GetCatalogOfferEvent: {ex}");
                Session.SendNotification("Error al cargar la oferta");
            }
        }
    }
}