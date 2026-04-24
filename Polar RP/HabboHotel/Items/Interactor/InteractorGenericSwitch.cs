using System;
using System.Collections.Generic;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Pathfinding;
using System.Linq;
using System.Drawing;

namespace Polar.HabboHotel.Items.Interactor
{
    public class InteractorGenericSwitch : IFurniInteractor
    {
        public void OnPlace(GameClient Session, Item Item)
        {
        }

        public void OnRemove(GameClient Session, Item Item)
        {
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            int Modes = Item.GetBaseItem().Modes - 1;

            if (Session == null || !HasRights || Modes <= 0)
                return;

            int CurrentMode = 0;
            int NewMode = 0;

            int.TryParse(Item.ExtraData, out CurrentMode);

            if (CurrentMode <= 0)
                NewMode = 1;
            else if (CurrentMode >= Modes)
                NewMode = 0;
            else
                NewMode = CurrentMode + 1;

            Item.ExtraData = NewMode.ToString();
            Item.UpdateState();

            // Handle items with adjustable heights (like pyramids or stacking tools)
            if (Item.GetBaseItem().AdjustableHeights.Count > 0)
            {
                Item.GetRoom().GetGameMap().UpdateMapForItem(Item);

                var usersOnTiles = new List<RoomUser>();
                foreach (var tile in Item.GetAffectedTiles)
                {
                    var user = Item.GetRoom().GetRoomUserManager().GetUserForSquare(tile.X, tile.Y);
                    if (user != null && !usersOnTiles.Contains(user))
                        usersOnTiles.Add(user);
                }

                foreach (var user in usersOnTiles)
                {
                    Item.GetRoom().GetRoomUserManager().UpdateUserStatus(user, false);
                }
            }
        }

        public void OnWiredTrigger(Item Item)
        {
            int Modes = Item.GetBaseItem().Modes - 1;

            if (Modes == 0)
            {
                return;
            }

            int CurrentMode = 0;
            int NewMode = 0;

            if (string.IsNullOrEmpty(Item.ExtraData))
                Item.ExtraData = "0";

            if (!int.TryParse(Item.ExtraData, out CurrentMode))
            {
                return;
            }

            if (CurrentMode <= 0)
            {
                NewMode = 1;
            }
            else if (CurrentMode >= Modes)
            {
                NewMode = 0;
            }
            else
            {
                NewMode = CurrentMode + 1;
            }

            Item.ExtraData = NewMode.ToString();
            Item.UpdateState();
        }
    }
}