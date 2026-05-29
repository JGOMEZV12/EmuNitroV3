using System;
using System.Collections.Generic;
using System.Linq;
using Polar.Core;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.GameClients;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class CatalogPageComposer : ServerPacket
    {
        // Puerto fiel del Java:
        // response.appendInt(page.getId())
        // response.appendString(mode)
        // page.serialize(response)           → template + strings de layout
        // appendInt(items.size()) + item.serialize() por cada item
        // appendInt(offerId)
        // appendBoolean(false)               → acceptSeasonCurrencyAsCredits
        // si es FrontPage → serializeExtra() → featured pages

        public CatalogPageComposer(CatalogPage page, GameClient client, int offerId, string mode)
            : base(ServerPacketHeader.CatalogPageMessageComposer)
        {
            if (page == null) throw new ArgumentNullException(nameof(page));

            // ── 1. Id + mode ─────────────────────────────────────────────────
            WriteInteger(page.Id);
            WriteString(mode ?? "NORMAL");

            // ── 2. page.serialize() → template + PageStrings1 + PageStrings2 ─
            page.Serialize(this);

            var items = page.Items?.Values?.Where(i => i != null && i.Data != null).ToList();
            if (items != null && items.Count > 0)
            {
                WriteInteger(items.Count);
                foreach (var item in items)
                    item.Serialize(this);
            }
            else
            {
                WriteInteger(0);
            }

                // ── 4. offerId + acceptSeasonCurrencyAsCredits ───────────────────
            WriteInteger(offerId);
            WriteBoolean(false); // acceptSeasonCurrencyAsCredits

            // ── 5. serializeExtra() si es FrontPage ──────────────────────────
            if (page.IsFrontPage)
                WriteFrontPageNotices();
        }

        private void WriteFrontPageNotices()
        {
            WriteInteger(4);

            WriteNotice(1, CatalogSettings.CATALOG_NOTICE_1, CatalogSettings.CATALOG_IMG_NOTICE_1, CatalogSettings.CATALOG_URL_NOTICE_1);
            WriteNotice(2, CatalogSettings.CATALOG_NOTICE_2, CatalogSettings.CATALOG_IMG_NOTICE_2, CatalogSettings.CATALOG_URL_NOTICE_2);
            WriteNotice(3, CatalogSettings.CATALOG_NOTICE_3, CatalogSettings.CATALOG_IMG_NOTICE_3, CatalogSettings.CATALOG_URL_NOTICE_3);
            WriteNotice(4, CatalogSettings.CATALOG_NOTICE_4, CatalogSettings.CATALOG_IMG_NOTICE_4, CatalogSettings.CATALOG_URL_NOTICE_4);
        }

        private void WriteNotice(int id, string text, string img, string url)
        {
            WriteInteger(id);
            WriteString(text ?? string.Empty);
            WriteString(img ?? string.Empty);
            WriteInteger(0);
            WriteString(url ?? string.Empty);
            WriteInteger(-1);
        }
    }
}