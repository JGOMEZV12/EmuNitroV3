using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.Items;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    public class PurchaseOKComposer : ServerPacket
    {
        public PurchaseOKComposer()
            : base(ServerPacketHeader.PurchaseOKMessageComposer)
        {
            WriteEmptyPurchase();
        }

        public PurchaseOKComposer(CatalogItem item, ItemData baseItem)
            : base(ServerPacketHeader.PurchaseOKMessageComposer)
        {
            if (item == null || baseItem == null)
            {
                WriteEmptyPurchase();
                return;
            }

            WritePurchase(item, baseItem);
        }

        private void WriteEmptyPurchase()
        {
            WriteInteger(0);
            WriteString("");
            WriteBoolean(false);
            WriteInteger(0);
            WriteInteger(0);
            WriteInteger(0);
            WriteBoolean(true);
            WriteInteger(1);
            WriteString("s");
            WriteInteger(0);
            WriteString("");
            WriteInteger(1);
            WriteInteger(0);
            WriteString("");
            WriteInteger(1);
        }

        private void WritePurchase(CatalogItem item, ItemData baseItem)
        {
            // 1. BaseItem ID (NO OfferId!)
            WriteInteger(baseItem.Id);

            // 2. BaseItem ItemName (NO CatalogItem.Name!)
            WriteString(baseItem.ItemName);

            // 3. Is rentable (always false)
            WriteBoolean(false);

            // 4. Cost in credits
            WriteInteger(item.CostCredits);

            // 5. Cost in pixels/duckets
            WriteInteger(item.CostPixels);

            // 6. Currency type (0 = duckets, 5 = diamonds)
            // IMPORTANTE: Tu versión siempre usa 0 aquí, incluso para diamantes
            WriteInteger(0);

            // 7. Can gift (siempre true en tu versión)
            WriteBoolean(true);

            // 8. Number of items (siempre 1 en tu versión)
            WriteInteger(1);

            // 9. Item type
            WriteString(baseItem.Type.ToString().ToLower());

            // 10. Sprite ID
            WriteInteger(baseItem.SpriteId);

            // 11. Extra data (siempre vacío en tu versión)
            WriteString("");

            // 12. Amount (siempre 1 en tu versión)
            WriteInteger(1);

            // 13. Unknown integer (0 en tu versión)
            WriteInteger(0);

            // 14. Unknown string (vacío en tu versión)
            WriteString("");

            // 15. Unknown integer (1 en tu versión)
            WriteInteger(1);
        }
    }
}