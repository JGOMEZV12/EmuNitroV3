using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.Food;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class PonerCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_fast_walk"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Agarra el chaleco antibalas que tienes en frente."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int FoodId = 0;
            Item Item = null;
            RoomUser User = Session.GetRoomUser();
            #endregion

            #region Conditions
            if (User == null)
                return;

            foreach (Item item in Room.GetRoomItemHandler().GetFloor)
            {
                if (item.GetX == User.SquareInFront.X && item.GetY == User.SquareInFront.Y)
                {
                    if (FoodManager.GetFood(item.BaseItem) != null)
                    {
                        Item = item;
                        FoodId = item.BaseItem;
                    }
                }
            }

            Food Food = FoodManager.GetFood(FoodId);
            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes comer comida mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("No puedes comer comida mientras estás encarcelado", 1);
                return;
            }

            if (!Session.GetRoleplay().NearItem("clothing_kevlar", 1))
            {
                Session.SendWhisper("¡Debes estar frente a un chaleco!");
                return;
            }

            if (Session.GetRoleplay().CurHealth > 200)
            {
                Session.SendWhisper("Ya tienes un chaleco, por favor intentalo más tarde...", 1);
                return;
            }

            #endregion

            #region Execute
            Session.Shout(" Se coloca un chaleco antibalas [300% Salud]", 4);
            Session.GetRoleplay().CurHealth = 300;
            Session.GetRoleplay().Armor = 100;
            HabboRoleplay.Misc.RoleplayManager.GetLookAndMotto(Session, "poof");
            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
            #endregion
        }
    }
}