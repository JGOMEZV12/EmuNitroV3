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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Restaurant
{
    class ServeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_restaurant_serve"; }
        }

        public string Parameters
        {
            get { return "%name%"; }
        }

        public string Description
        {
            get { return "Sirve la comida o la bebida deseada delante de usted."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            RoomUser User = Session.GetRoomUser();
            Group Group = GroupManager.GetJob(Session.GetRoleplay().JobId);
            GroupRank GroupRank = GroupManager.GetJobRank(Session.GetRoleplay().JobId, Session.GetRoleplay().JobRank);
            #endregion

            #region Conditions
            if (User == null)
                return;

            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor escriba :servir (item) Sólo puede servir los siguientes elementos: " + FoodManager.GetServableItems(Session) + "!", 1);
                return;
            }

            // ── Subcomando: menu ─────────────────────────────────────────────
            if (Params[1].ToLower() == "menu")
            {
                ShowFoodMenu(Session);
                return;
            }

            string FoodName = Params[1].ToString();
            Food Food = FoodManager.GetFoodTwo(FoodName);

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("Usted debe estar trabajando para hacer esto trabajando", 1);
                return;
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes servir comida o bebidas mientras estés muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes servir comida o bebidas mientras estás encarcelado!", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "serve"))
            {
                Session.SendWhisper("¡No tienes permiso para usar este comando!", 1);
                return;
            }

            if (!GroupRank.CanWorkHere(Session.GetHabbo().CurrentRoomId))
            {
                Session.SendWhisper("Esta no es una de sus salas de trabajo, Sólo puede trabajar en RoomID (s): " + String.Join(",", GroupRank.WorkRooms) + ".", 1);
                return;
            }

            if (Food == null)
            {
                Session.SendWhisper("¡Esto no es un tipo válido de comida o bebida! Sólo puede servir: " + FoodManager.GetServableItems(Session) + "!", 1);
                return;
            }

            if (!FoodManager.CanServe(User))
            {
                Session.SendWhisper("¡Encuentra una buena mesa para servir!", 1);
                return;
            }
            #endregion

            #region Execute
            double MaxHeight = 0.0;
            Item ItemInFront;
            if (Room.GetGameMap().GetHighestItemForSquare(User.SquareInFront, out ItemInFront))
            {
                if (ItemInFront != null)
                    MaxHeight = ItemInFront.TotalHeight;
            }

            Session.Shout(Food.ServeText, 4);
            RoleplayManager.PlaceItemToRoom(Session, Food.ItemId, 0, User.SquareInFront.X, User.SquareInFront.Y, MaxHeight, User.RotBody, false, Room.Id, false, Food.ExtraData, true);
            #endregion
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Muestra una notificación con todos los alimentos y bebidas registrados en la DB.
        /// Uso: :servir menu
        /// </summary>
        private void ShowFoodMenu(GameClients.GameClient Session)
        {
            var foods = new List<Food>();
            var drinks = new List<Food>();

            foreach (var item in FoodManager.FoodList.Values)
            {
                if (item.Type.ToLower() == "drink")
                    drinks.Add(item);
                else
                    foods.Add(item);
            }

            var sb = new StringBuilder();
            sb.Append("---------- Menú del Restaurante ----------\n\n");

            sb.Append("🍽 Comidas (" + foods.Count + "):\n");
            if (foods.Count > 0)
                sb.Append("  " + string.Join(", ", foods.Select(f => f.Name)) + "\n");
            else
                sb.Append("  (ninguna)\n");

            sb.Append("\n");

            sb.Append("🥤 Bebidas (" + drinks.Count + "):\n");
            if (drinks.Count > 0)
                sb.Append("  " + string.Join(", ", drinks.Select(d => d.Name)) + "\n");
            else
                sb.Append("  (ninguna)\n");

            sb.Append("\n");
            sb.Append("Usa :servir (nombre) para servir un ítem.");

            Session.SendNotification(sb.ToString());
        }
    }
}