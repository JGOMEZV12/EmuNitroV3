using Polar.Core;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;
using Polar.Utilities;
using System;
using System.Collections.Generic;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class CatalogIndexComposer : ServerPacket
    {
        private const int MAX_OFFERS = 1000;
        private const int MAX_CHILDREN = 500;
        private const int MAX_DEPTH = 20;

        private readonly GameClient _session;

        public CatalogIndexComposer(GameClient session, ICollection<CatalogPage> pages, string mode)
            : base(ServerPacketHeader.CatalogIndexMessageComposer)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));

            WriteCatalogIndex(pages, mode);
        }

        private void WriteCatalogIndex(ICollection<CatalogPage> pages, string mode)
        {
            // Nodo raíz (igual que el Java)
            WriteBoolean(true);     // catálogo activado
            WriteInteger(0);        // créditos del usuario (unused pero requerido)
            WriteInteger(-1);       // puntos seasonales (-1 = no mostrar)
            WriteString("root");    // nombre del nodo raíz
            WriteString(string.Empty); // localization del nodo raíz
            WriteInteger(0);        // offerIds del nodo raíz (ninguno)

            // Cantidad de páginas hijas del nodo raíz
            int childCount = Math.Min(pages?.Count ?? 0, MAX_CHILDREN);
            WriteInteger(childCount);

            if (pages != null)
            {
                int written = 0;
                foreach (var page in pages)
                {
                    if (written >= childCount) break;
                    if (page != null)
                    {
                        AppendPage(page, depth: 1);
                        written++;
                    }
                }
            }

            // Footer
            WriteBoolean(false);    // acceptSeasonCurrencyAsCredits
            WriteString(mode ?? "NORMAL");
        }

        private void AppendPage(CatalogPage page, int depth)
        {
            if (page == null) return;

            // Obtener subpáginas
            ICollection<CatalogPage> subPages = null;
            try
            {
                subPages = PolarEnvironment.GetGame().GetCatalog().GetPages(_session, page.Id);
            }
            catch (Exception ex)
            {
                Logging.LogException("CatalogIndexComposer.AppendPage: " + ex);
            }

            // Datos del nodo (misma secuencia que el Java)
            WriteBoolean(page.Visible);
            WriteInteger(page.Icon);
            WriteInteger(page.Enabled ? page.Id : -1);
            WriteString(page.PageLink ?? string.Empty); // pageName
            WriteString(page.Caption ?? string.Empty); // localization / caption

            // offerIds
            var offerIds = page.ItemOffers?.Keys;
            int offerCount = Math.Min(offerIds?.Count ?? 0, MAX_OFFERS);
            WriteInteger(offerCount);
            if (offerIds != null)
            {
                int written = 0;
                foreach (var key in offerIds)
                {
                    if (written >= offerCount) break;
                    WriteInteger(key);
                    written++;
                }
            }

            // Subpáginas (con límite de profundidad igual que el Java)
            if (depth >= MAX_DEPTH)
            {
                WriteInteger(0);
                return;
            }

            int childCount = Math.Min(subPages?.Count ?? 0, MAX_CHILDREN);
            WriteInteger(childCount);

            if (subPages != null)
            {
                int written = 0;
                foreach (var sub in subPages)
                {
                    if (written >= childCount) break;
                    if (sub != null)
                    {
                        AppendPage(sub, depth + 1);
                        written++;
                    }
                }
            }
        }
    }
}