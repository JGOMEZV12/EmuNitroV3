using System;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using System.Drawing;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorMultiHeight : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item) { }
        public void OnRemove(GameClient Session, Item Item) { }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            int Modes = Item.GetBaseItem().Modes - 1;
            if (Session == null || !HasRights || Modes <= 0)
                return;

            int CurrentMode = 0;
            int.TryParse(Item.ExtraData, out CurrentMode);

            int NewMode;
            if (CurrentMode <= 0) NewMode = 1;
            else if (CurrentMode >= Modes) NewMode = 0;
            else NewMode = CurrentMode + 1;

            Item.ExtraData = NewMode.ToString();

            // ✅ Java: updateTiles — recalcular Z desde AdjustableHeights[newMode]
            var baseItem = Item.GetBaseItem();
            if (baseItem.AdjustableHeights != null &&
                baseItem.AdjustableHeights.TryGetValue(Item.ExtraData, out double newHeight))
            {
                // Actualizar la Z del item en la DB y en el mapa
                Item.GetZ = Item.GetZ - (baseItem.AdjustableHeights.TryGetValue(
                    CurrentMode.ToString(), out double oldHeight) ? oldHeight : 0) + newHeight;

                Item.SetState(Item.GetX, Item.GetY, Item.GetZ,
                    Gamemap.GetAffectedTiles(baseItem.Length, baseItem.Width,
                        Item.GetX, Item.GetY, Item.Rotation));

                Item.GetRoom().GetRoomItemHandler().UpdateItem(Item);
            }

            Item.UpdateState();
            Item.GetRoom().GetGameMap().UpdateMapForItem(Item);

            // Actualizar usuarios encima
            foreach (var tile in Item.GetAffectedTiles)
            {
                var user = Item.GetRoom().GetRoomUserManager().GetUserForSquare(tile.X, tile.Y);
                if (user != null)
                    Item.GetRoom().GetRoomUserManager().UpdateUserStatus(user, false);
            }
        }


        public void OnWiredTrigger(Item Item)
        {
            var baseItem = Item.GetBaseItem();
            int Modes = baseItem.AdjustableHeights != null && baseItem.AdjustableHeights.Count > 1
                ? baseItem.AdjustableHeights.Count - 1
                : baseItem.Modes - 1;

            if (Modes <= 0) return;

            if (string.IsNullOrEmpty(Item.ExtraData))
                Item.ExtraData = "0";

            if (!int.TryParse(Item.ExtraData, out int CurrentMode)) return;

            int NewMode;
            if (CurrentMode <= 0)
                NewMode = 1;
            else if (CurrentMode >= Modes)
                NewMode = 0;
            else
                NewMode = CurrentMode + 1;

            Item.ExtraData = NewMode.ToString();
            Item.UpdateState();

            if (baseItem.AdjustableHeights != null && baseItem.AdjustableHeights.Count > 0)
            {
                Item.GetRoom()?.GetGameMap()?.UpdateMapForItem(Item);
            }
        }
    }
}