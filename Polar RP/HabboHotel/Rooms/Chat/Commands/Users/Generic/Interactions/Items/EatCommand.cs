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
    class EatCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_eat"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Come el plato de comida delante de usted."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            bool Stolen = false;
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

            if (Food == null || Item == null)
            {
                Session.SendWhisper("No hay comida delante de usted!", 1);
                return;
            }

            if (Food.Type != "food")
            {
                if (Food.Type == "drink")
                {
                    Session.SendWhisper("Usa el comando :beber para beber esto!", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("No puedes comer esto!", 1);
                    return;
                }
            }

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

            if (Session.GetRoleplay().Hunger <= 0)
            {
                Session.SendWhisper("Su estómago está demasiado lleno para tener más comida", 1);
                return;
            }

            if (Food.Cost > 0)
            {
                if (Session.GetHabbo().Credits < Food.Cost)
                {
                    if (Session.GetRoleplay().RobItem != Item)
                    {
                        Session.GetRoleplay().RobItem = Item;
                        Session.SendWhisper("Usted no tiene suficiente dinero para permitirse comer esta comida! Tal vez si lo intentas de nuevo...", 1);
                        return;
                    }
                    else
                    {
                        Session.GetRoleplay().RobItem = null;
                        Session.SendWhisper("Te comiste la comida sin pagar por ello! ¡Cuidado si alguien te vio!", 1);
                        Stolen = true;
                    }
                }
            }
            #endregion

            #region Execute
            string EatText = Food.EatText;

            if (Food.Health == 0)
                EatText = EatText.Replace("[HEALTH]", "");
            else
                EatText = EatText.Replace("[HEALTH]", "[+" + Food.Health + " HP]");

            if (Food.Energy == 0)
                EatText = EatText.Replace("[ENERGY]", "");
            else
                EatText = EatText.Replace("[ENERGY]", "[+" + Food.Energy + " E]");

            if (Food.Cost == 0)
                EatText = EatText.Replace("[COST]", "");
            else
                EatText = EatText.Replace("[COST]", "[-$" + Food.Cost + "]");

            if (Food.Hunger == 0)
                EatText = EatText.Replace("[HUNGER]", "");
            else
                EatText = EatText.Replace("[HUNGER]", "[-" + Food.Hunger + " Hunger]");

            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Eating", 1);
            PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.EAT_FOOD);

            if (Food.Hunger > 0)
            {
                int HungerChange = Session.GetRoleplay().Hunger - Food.Hunger;

                if (HungerChange <= 0)
                    Session.GetRoleplay().Hunger = 0;
                else
                    Session.GetRoleplay().Hunger = HungerChange;
            }

            if (Food.Energy > 0)
            {
                int EnergyChange = Session.GetRoleplay().CurEnergy + Food.Energy;

                if (EnergyChange >= Session.GetRoleplay().MaxEnergy)
                    Session.GetRoleplay().CurEnergy = Session.GetRoleplay().MaxEnergy;
                else
                    Session.GetRoleplay().CurEnergy = EnergyChange;
            }

            if (Food.Health > 0)
            {
                int HealthChange = Session.GetRoleplay().CurHealth + Food.Health;

                if (HealthChange >= Session.GetRoleplay().MaxHealth)
                    Session.GetRoleplay().CurHealth = Session.GetRoleplay().MaxHealth;
                else
                    Session.GetRoleplay().CurHealth = HealthChange;
            }

            if (Food.Cost > 0 && !Stolen)
            {
                Session.GetHabbo().Credits -= Food.Cost;
                Session.GetHabbo().UpdateCreditsBalance();
            }

            string FoodName = Food.Name.Substring(0, 1).ToUpper() + Food.Name.Substring(1);

            if (Stolen)
            {
                Session.Shout("*Rápidamente come " + FoodName + " sin pagar*", 4);

                if (!Session.GetRoleplay().WantedFor.Contains("stealing food"))
                    Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "stealing food, ";
            }
            else
                Session.Shout(EatText, 4);

            if (Item.InteractingUser > 0 && !Stolen)
            {
                var Server = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Item.InteractingUser);

                if (Server != null)
                {
                    if (Session != Server)
                    {
                        Server.GetHabbo().Credits += 100;
                        Server.GetHabbo().UpdateCreditsBalance();
                        Server.SendWhisper("Has ganado $ 100 extra en consejos para servir algunos alimentos " + Session.GetHabbo().Username + "!", 1);
                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Server, "ACH_ServingFood", 1);
                    }
                }

                Item.InteractingUser = 0;
            }

            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
            #endregion
        }
    }
}