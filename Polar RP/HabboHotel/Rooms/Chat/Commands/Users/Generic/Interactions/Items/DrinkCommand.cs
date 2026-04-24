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
    class DrinkCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_interactions_drink"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Bebe la bebida delante de usted."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            bool Stolen = false;
            int DrinkId = 0;
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
                        DrinkId = item.BaseItem;
                    }
                }
            }

            Food Food = FoodManager.GetFood(DrinkId);

            if (Food == null || Item == null)
            {
                Session.SendWhisper("No hay bebida frente a usted", 1);
                return;
            }

            if (Food.Type != "drink")
            {
                if (Food.Type == "food")
                {
                    Session.SendWhisper("Utilice el comando :comer para comer esto ", 1);
                    return;
                }
                else
                {
                    Session.SendWhisper("¡No puedes beber esto!", 1);
                    return;
                }
            }

            if (Session.GetRoleplay().IsDead)
            {
                Session.SendWhisper("¡No puedes beber cosas mientras estás muerto!", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorking == true)
            {
                Session.SendWhisper("¡Deja de trabajar para beber! No querrás ensuciar el uniforme", 1);
                return;
            }

            if (Session.GetRoleplay().IsJailed)
            {
                Session.SendWhisper("¡No puedes beber cosas mientras estás encarcelado!", 1);
                return;
            }

            if (Session.GetRoleplay().CurEnergy >= Session.GetRoleplay().MaxEnergy)
            {
                Session.SendWhisper("¡Su energía ya está full y no tienes sed!", 1);
                return;
            }

            if (Food.Cost > 0)
            {
                if (Session.GetHabbo().Credits < Food.Cost)
                {
                    if (Session.GetRoleplay().RobItem != Item)
                    {
                        Session.GetRoleplay().RobItem = Item;
                        Session.SendWhisper("¡Usted no tiene bastante dinero para poder beber esto! Tal vez si lo intentas de nuevo...", 1);
                        return;
                    }
                    else
                    {
                        Session.GetRoleplay().RobItem = null;
                        Session.SendWhisper("Usted bebió la bebida sin pagar por ello! ¡Cuidado si alguien te vio!", 1);
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

            if (Food.Alcohol == 0)
                EatText = EatText.Replace("[ALCOHOL]", "");
            else
                EatText = EatText.Replace("[ALCOHOL]", "[+" + Food.Alcohol + " A]");

            if (Food.Cost == 0)
                EatText = EatText.Replace("[COST]", "");
            else
                EatText = EatText.Replace("[COST]", "[-$" + Food.Cost + "]");

            if (Food.Hunger == 0)
                EatText = EatText.Replace("[HUNGER]", "");
            else
                EatText = EatText.Replace("[HUNGER]", "[-" + Food.Hunger + " Hunger]");

            if (!Food.Name.ToLower().Contains("bleach"))
            {
                PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_Drinking", 1);
                PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Session, QuestType.DRINK_DRINK);
            }

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

            if (Food.Alcohol > 0)
            {
                int AlcoholChange = Session.GetRoleplay().CurAlcohol + Food.Alcohol;

                if (AlcoholChange >= Session.GetRoleplay().MaxAlcohol)
                    Session.GetRoleplay().CurAlcohol = Session.GetRoleplay().MaxAlcohol;
                else
               
                    User.CarryItem(50);
                    Session.GetRoleplay().CurAlcohol = AlcoholChange;
                    Session.GetRoleplay().LevelEXP += 5;
                    Session.GetRoleplay().StrengthEXP += 1;

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

            if (FoodName.ToLower().Contains("bleach"))
            {
                Session.Shout(EatText, 4);

                // kill the ciudadano, haha
                Session.GetRoleplay().CurHealth = 0;
            }
            else
            {
                if (Stolen)
                {
                    Session.Shout("*Rápidamente bebe el " + FoodName + " Sin pagar por ello*", 4);

                    if (!Session.GetRoleplay().WantedFor.Contains("Robar bebidas"))
                        Session.GetRoleplay().WantedFor = Session.GetRoleplay().WantedFor + "Robar bebidas, ";
                }
                else
                    Session.Shout(EatText, 4);
            }

            if (Item.InteractingUser > 0 && !Stolen)
            {
                var Server = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Item.InteractingUser);

                if (Server != null)
                {
                    if (Session != Server)
                    {
                        Server.GetHabbo().Credits += 30;
                        Server.GetHabbo().UpdateCreditsBalance();
                        Server.SendWhisper("Te has ganado un extra de $ 30 en consejos para servir algunas bebidas a " + Session.GetHabbo().Username + "!", 1);
                        PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Server, "ACH_ServingDrinks", 1);
                    }
                }

                Item.InteractingUser = 0;
            }

            Room.GetRoomItemHandler().RemoveFurniture(Session, Item.Id);
            #endregion
        }
    }
}