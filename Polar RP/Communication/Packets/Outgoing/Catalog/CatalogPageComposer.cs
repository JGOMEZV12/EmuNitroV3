using System;
using System.Linq;
using Polar.Core;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.Items.Utilities;
using Polar.HabboHotel.Catalog.Utilities;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class CatalogPageComposer : ServerPacket
    {
        public CatalogPageComposer(CatalogPage page, string catalogMode)
            : base(ServerPacketHeader.CatalogPageMessageComposer)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            WritePageHeader(page, catalogMode);

            if (ShouldWriteItems(page))
            {
                WritePageItems(page);
            }
            else
            {
                WriteInteger(0);
            }

            WritePageFooter(page);
        }

        private void WritePageHeader(CatalogPage page, string catalogMode)
        {
            WriteInteger(page.Id);
            WriteString(catalogMode ?? "NORMAL");
            WriteString(page.Template ?? "default");

            // Page strings 1
            WriteInteger(page.PageStrings1?.Count ?? 0);
            if (page.PageStrings1 != null)
            {
                foreach (string s in page.PageStrings1.Where(s => s != null))
                {
                    WriteString(s);
                }
            }

            // Page strings 2
            WriteInteger(page.PageStrings2?.Count ?? 0);
            if (page.PageStrings2 != null)
            {
                foreach (string s in page.PageStrings2.Where(s => s != null))
                {
                    WriteString(s);
                }
            }
        }

        private bool ShouldWriteItems(CatalogPage page)
        {
            return !page.Template.Equals("frontpage", StringComparison.OrdinalIgnoreCase)
                && !page.Template.Equals("frontpage4", StringComparison.OrdinalIgnoreCase)
                && !page.Template.Equals("frontpage_featured", StringComparison.OrdinalIgnoreCase)
                && !page.Template.Equals("club_buy", StringComparison.OrdinalIgnoreCase);
        }

        private void WritePageItems(CatalogPage page)
        {
            var items = page.Items?.Values?.Where(i => i != null && i.Data != null).ToList();

            if (page.Template == "name_color" || page.Template == "name_colors" || page.Template == "name_prefix" || page.Template == "name_prefixes")
            {
                WriteInteger(items?.Count ?? 0);
                foreach (CatalogItem item in items)
                {
                    WriteInteger(item.Id);
                    WriteString(item.Name); // Color code (HEX) or Prefix string
                    WriteBoolean(false);
                    WriteInteger(item.CostCredits);
                    WriteInteger(item.CostPixels);
                    WriteInteger(0);
                    WriteBoolean(false);
                    WriteInteger(1);
                    WriteString("s");
                    WriteInteger(0);
                    WriteString("");
                    WriteInteger(1);
                    WriteBoolean(false);
                    WriteInteger(0);
                    WriteBoolean(true);
                    WriteString("");
                }
                return;
            }

            WriteInteger(items?.Count ?? 0);

            if (items == null) return;

            foreach (CatalogItem item in items)
            {
                WriteCatalogItem(item);
            }
        }

        private void WriteCatalogItem(CatalogItem item)
        {
            WriteInteger(item.Id);
            WriteString(item.Name ?? string.Empty);
            WriteBoolean(false); // IsRentable

            // Costos
            WriteInteger(item.CostCredits);

            if (item.CostDiamonds > 0)
            {
                WriteInteger(item.CostDiamonds);
                WriteInteger(5); // Diamonds
            }
            else
            {
                WriteInteger(item.CostPixels);
                WriteInteger(0); // Type of PixelCost
            }

            WriteBoolean(ItemUtility.CanGiftItem(item));

            // Determinar count (1 para items normales, 2 si tiene badge)
            int itemCount = string.IsNullOrEmpty(item.Badge) ? 1 : 2;
            WriteInteger(itemCount);

            // Escribir badge si existe
            if (!string.IsNullOrEmpty(item.Badge))
            {
                WriteString("b");
                WriteString(item.Badge);
            }

            // Escribir datos del item
            WriteItemData(item);
        }

        private void WriteItemData(CatalogItem item)
        {
            string itemType = item.Data.Type.ToString();
            WriteString(itemType);

            if (itemType.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                // Badge
                WriteString(item.Data.ItemName ?? string.Empty);
            }
            else
            {
                WriteInteger(item.Data.SpriteId);

                // Extra data basado en el tipo de interacción
                string extraData = GetItemExtraData(item);
                if (item.Name.Contains("bot") && item.GetBaseItem().Type == 'r')
                {
                    WriteString(GetBotExtraData(item.ItemId));
                }
                else
                {
                    WriteString(extraData ?? string.Empty);
                }

                WriteInteger(item.Amount);
                WriteBoolean(item.IsLimited);

                if (item.IsLimited)
                {
                    WriteInteger(item.LimitedEditionStack);
                    WriteInteger(Math.Max(0, item.LimitedEditionStack - item.LimitedEditionSells));
                }
            }

            WriteInteger(0); // club_level
            WriteBoolean(ItemUtility.CanSelectAmount(item));
            WriteBoolean(true); // unknown, usualmente true
            WriteString(""); // extra data 2
        }

        private string GetItemExtraData(CatalogItem item)
        {
            if (item.Data == null) return string.Empty;

            switch (item.Data.InteractionType)
            {
                case InteractionType.WALLPAPER:
                case InteractionType.FLOOR:
                case InteractionType.LANDSCAPE:
                    return GetWallFloorLandscapeExtraData(item.Name);

                case InteractionType.BOT:
                    return GetBotExtraData(item.ItemId);

                default:
                    return item.ExtraData ?? string.Empty;
            }
        }

        private string GetWallFloorLandscapeExtraData(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return string.Empty;

            var parts = itemName.Split('_');
            return parts.Length >= 3 ? parts[2] : string.Empty;
        }

        private string GetBotExtraData(int itemId)
        {
            if (PolarEnvironment.GetGame().GetCatalog().TryGetBot(itemId, out CatalogBot catalogBot))
            {
                return catalogBot?.Figure ?? "hd-180-7.ea-1406-62.ch-210-1321.hr-831-49.ca-1813-62.sh-295-1321.lg-285-92";
            }

            return "hd-180-7.ea-1406-62.ch-210-1321.hr-831-49.ca-1813-62.sh-295-1321.lg-285-92";
        }

        private void WritePageFooter(CatalogPage page)
        {
            WriteInteger(-1); // unknown
            WriteBoolean(false); // unknown

            if (page.Template.Equals("frontpage4", StringComparison.OrdinalIgnoreCase))
            {
                WriteFrontPageNotices();
            }
        }

        private void WriteFrontPageNotices()
        {
            WriteInteger(4); // count

            // Notice 1
            WriteInteger(1);
            WriteString(CatalogSettings.CATALOG_NOTICE_1 ?? "New!");
            WriteString(CatalogSettings.CATALOG_IMG_NOTICE_1 ?? "");
            WriteInteger(0);
            WriteString(CatalogSettings.CATALOG_URL_NOTICE_1 ?? "");
            WriteInteger(-1);

            // Notice 2
            WriteInteger(2);
            WriteString(CatalogSettings.CATALOG_NOTICE_2 ?? "Hot!");
            WriteString(CatalogSettings.CATALOG_IMG_NOTICE_2 ?? "");
            WriteInteger(0);
            WriteString(CatalogSettings.CATALOG_URL_NOTICE_2 ?? "");
            WriteInteger(-1);

            // Notice 3
            WriteInteger(3);
            WriteString(CatalogSettings.CATALOG_NOTICE_3 ?? "Featured");
            WriteString(CatalogSettings.CATALOG_IMG_NOTICE_3 ?? "");
            WriteInteger(0);
            WriteString(CatalogSettings.CATALOG_URL_NOTICE_3 ?? "");
            WriteInteger(-1);

            // Notice 4
            WriteInteger(4);
            WriteString(CatalogSettings.CATALOG_NOTICE_4 ?? "Limited");
            WriteString(CatalogSettings.CATALOG_IMG_NOTICE_4 ?? "");
            WriteInteger(0);
            WriteString(CatalogSettings.CATALOG_URL_NOTICE_4 ?? "");
            WriteInteger(-1);
        }
    }
}