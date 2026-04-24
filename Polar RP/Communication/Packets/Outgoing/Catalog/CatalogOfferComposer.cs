using Polar.Core;
using Polar.HabboHotel.Catalog;
using Polar.HabboHotel.Catalog.Utilities;
using Polar.HabboHotel.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Communication.Packets.Outgoing.Catalog
{
    class CatalogOfferComposer : ServerPacket
    {
        public CatalogOfferComposer(CatalogItem Item)
            : base(ServerPacketHeader.CatalogOfferMessageComposer)
        {
            //Logging.WriteLine($"Creando CatalogOfferComposer para item: {Item.Id}, Tipo: {Item.Data.Type}");
            WriteInteger(Item.OfferId);
            WriteString(Item.Name);
            WriteBoolean(false); // IsRentable
            WriteInteger(Item.CostCredits);

            if (Item.CostDiamonds > 0)
            {
                WriteInteger(Item.CostDiamonds);
                WriteInteger(105); // Tipo de moneda: Diamantes
            }
            else
            {
                WriteInteger(Item.CostPixels);
                WriteInteger(0); // Tipo de moneda: Duckets/Pixels
            }

            WriteBoolean(ItemUtility.CanGiftItem(Item));

            // Contar cuántos elementos incluye esta oferta
            int itemCount = string.IsNullOrEmpty(Item.Badge) ? 1 : 2;
            WriteInteger(itemCount);

            // Primer elemento: El item principal
            WriteString(Item.Data.Type.ToString().ToLower()); // "s", "e", "r", "b", "p"

            if (Item.Data.Type.ToString().ToLower() == "b") // Badge
            {
                WriteString(Item.Data.ItemName); // Badge code
            }
            else
            {
                WriteInteger(Item.Data.SpriteId);

                // ExtraData depende del tipo de item
                string extraData = Item.ExtraData ?? string.Empty;

                if (Item.Data.InteractionType == InteractionType.WALLPAPER ||
                    Item.Data.InteractionType == InteractionType.FLOOR ||
                    Item.Data.InteractionType == InteractionType.LANDSCAPE)
                {
                    // Para fondos, pisos y paisajes
                    WriteString(Item.Name.Contains('_') ? Item.Name.Split('_')[2] : "0");
                }
                else if (Item.Data.InteractionType == InteractionType.BOT) // Bots
                {
                    if (!PolarEnvironment.GetGame().GetCatalog().TryGetBot(Item.ItemId, out var catalogBot))
                    {
                        WriteString("hd-180-7.ea-1406-62.ch-210-1321.hr-831-49.ca-1813-62.sh-295-1321.lg-285-92");
                    }
                    else
                    {
                        WriteString(catalogBot.Figure);
                    }
                }
                else
                {
                    WriteString(extraData);
                }

                WriteInteger(Item.Amount);
                WriteBoolean(Item.IsLimited);

                if (Item.IsLimited)
                {
                    WriteInteger(Item.LimitedEditionStack);
                    WriteInteger(Item.LimitedEditionStack - Item.LimitedEditionSells);
                }
            }

            // Segundo elemento: Badge (si aplica)
            if (!string.IsNullOrEmpty(Item.Badge))
            {
                WriteString("b"); // Tipo badge
                WriteString(Item.Badge); // Código del badge
            }

            WriteInteger(0); // club_level (0 = no requiere HC)
            WriteBoolean(ItemUtility.CanSelectAmount(Item));
            WriteBoolean(false); // "purchaseAsGift" (siempre false por ahora)
            WriteString(""); // previewImage
        }
    }
}