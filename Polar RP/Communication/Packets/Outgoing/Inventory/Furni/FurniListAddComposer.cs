using System;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Catalog.Utilities;

namespace Polar.Communication.Packets.Outgoing.Inventory.Furni
{
    internal class FurniListAddComposer : ServerPacket
    {
        public Item Item { get; }

        public FurniListAddComposer(Item item)
            : base(ServerPacketHeader.FurniListAddMessageComposer)
        {
            Item = item;
            string itemName = item.GetBaseItem().ItemName;

            // Java campo 1: giftAdjustedId
            // Reemplazar por item.GiftAdjustedId si tu modelo lo expone
            WriteInteger(item.Id);

            // Java campo 2: type.code
            WriteString(item.GetBaseItem().Type.ToString().ToUpper());

            // Java campo 3: ID real
            WriteInteger(item.Id);

            // Java campo 4: spriteId
            WriteInteger(item.GetBaseItem().SpriteId);

            // ---- Int categórico para items especiales por nombre ----
            // Java escribe este switch ANTES de la rama limited/normal
            switch (itemName)
            {
                case "landscape": WriteInteger(4); break;
                case "floor": WriteInteger(3); break;
                case "wallpaper": WriteInteger(2); break;
                case "poster": WriteInteger(6); break;
            }

            // ---- Extradata ----
            if (item.LimitedNo > 0)
            {
                // Java rama limited:
                // appendInt(1) + appendInt(256) + string extradata + limitedSells + limitedStack
                WriteInteger(1);
                WriteInteger(256);
                WriteString(item.ExtraData ?? string.Empty);
                WriteInteger(item.LimitedNo);
                WriteInteger(item.LimitedTot);
            }
            else
                ItemBehaviourUtility.GenerateExtradata(Item, this);

            // Flags comunes
            WriteBoolean(item.GetBaseItem().AllowEcotronRecycle);
            WriteBoolean(item.GetBaseItem().AllowTrade);
            // Java: !isLimited() && allowInventoryStack()
            WriteBoolean(item.LimitedNo == 0 && item.GetBaseItem().AllowInventoryStack);
            // Java: allowMarketplace() — NO IsRare
            WriteBoolean(item.GetBaseItem().AllowMarketplaceSell);
            WriteInteger(-1);       // segundos para expiración
            WriteBoolean(false);    // Java: false — el original tenía true
            WriteInteger(-1);       // RoomId

            // Footer exclusivo de floor items (Java: FurnitureType.FLOOR)
            if (!item.IsWallItem)
            {
                WriteString(string.Empty); // slotId
                WriteInteger(0);
            }

            // Java escribe int 100 al final — el original no lo tenía
            WriteInteger(100);
        }
    }
}