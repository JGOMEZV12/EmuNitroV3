using System;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Food;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Quests;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class DrinkServerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public DrinkServerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {
            //OnDuty = false;
            this.StartActivities();
        }

        public override void OnDeath(GameClient Client)
        {

        }

        public override void OnArrest(GameClient Client)
        {

        }

        public override void OnAttacked(GameClient Client)
        {

        }

        public override void OnUserLeaveRoom(GameClient Client)
        {
            if (!OnDuty)
                return;
        }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty)
                return;

            if (!GetRoomUser().IsWalking)
            {
                // Look at the user 
            }
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty)
                return;

            if (Client == null) return;
            if (Client.GetRoomUser() == null) return;

            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            GameClient Client = User.GetClient();

            if (Client == null)
                return;
            HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty)
                return;

            if (User.GetClient() == null)
                return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;
        }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty)
                return;

            if (RespondToSpeech(Client, Message))
                return;

            string Name = GetBotRoleplay().Name.ToLower();
            var ServableFoods = FoodManager.GetServableBotItems("drink");

            #region Serving Drinks
            if (Message.ToLower().Contains("servir") && Message.ToLower() != "servir")
            {
                string[] Params = Message.Split(' ');

                if (Params.Length == 1)
                    return;
                else
                {
                    if (Client.GetRoleplay().CurEnergy >= Client.GetRoleplay().MaxEnergy)
                    {
                        string WhisperMessage = "No tienes sed en absoluto " + Client.GetHabbo().Username + "!";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    if (Client.GetRoleplay().CurAlcohol >= Client.GetRoleplay().MaxAlcohol)
                    {
                        string WhisperMessage = "* " + Client.GetHabbo().Username + " te tomaste hasta el agua del florero, ya vete. Estas muy borracho y espantas a mis clientes*";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    if (GetBotRoleplay().WalkingToItem)
                    {
                        string WhisperMessage = "Ya estoy en mi camino para servir a alguien! Por favor, espere hasta que esté libre.";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        return;
                    }

                    string DesiredFood = Params[1].ToLower();
                    var Food = FoodManager.GetDrink(DesiredFood);

                    if (Food == null)
                    {
                        string WhisperMessage = "Esta bebida no existe, Por favor escriba 'bebidas' para ver qué puedo ofrecerle.";
                        Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    }
                    else
                    {
                        if (FoodManager.CanServe(Client.GetRoomUser()))
                            BeginServingFood(Food, Client);
                        else
                        {
                            string WhisperMessage = "¡Por favor, encuentre una mesa vacía para servirle lo que pide!";
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                        }
                    }
                }
            }
            #endregion

            else if (Message.ToLower() == Name)
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Quieres beber o comer algo?", true);
            else
                switch (Message.ToLower())
                {
                    #region Drinks List
                    case "drinks":
                    case "drink":
                    case "energy":
                    case "serve":
                    case "bebidas":
                        {
                            if (Client.GetRoleplay().CurEnergy >= Client.GetRoleplay().MaxEnergy)
                            {
                                string WhisperMessage = "No tienes sed en absoluto " + Client.GetHabbo().Username + "!";
                                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                            }
                            else
                            {
                                GetRoomUser().Chat("¡BIENVENIDO! " + Client.GetHabbo().Username + ", Te ves sediento ¿Qué le gustaría beber?", true);

                                StringBuilder FoodList = new StringBuilder().Append("--- Bebidas Servibles---\n");
                                FoodList.Append("Para solicitar cualquiera de los siguientes, escriba 'serve <drink name>' with an empty table infront of you!\n\n");

                                foreach (var Food in ServableFoods.OrderBy(x => x.Cost))
                                {
                                    if (Food != null)
                                    {
                                        string RealName = Food.Name.Substring(0, 1).ToUpper() + Food.Name.Substring(1);

                                        FoodList.Append("--- " + RealName + " ---\n");
                                        FoodList.Append("Precio: $" + Food.Cost + "  | Hambre: -" + Food.Hunger + "\n");
                                        FoodList.Append("Salud: +" + Food.Health + " | Energia: +" + Food.Energy + "\n\n");
                                    }
                                }

                                Client.SendMessage(new MOTDNotificationComposer(FoodList.ToString()));
                            }
                            break;
                        }
                        #endregion
                }
        }

        public override void StopActivities()
        {
            if (!OnDuty)
                return;

            if (GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("trabajar"))
                GetBotRoleplay().TimerManager.ActiveTimers["trabajar"].EndTimer();

            if (GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("serving"))
                GetBotRoleplay().TimerManager.ActiveTimers["serving"].EndTimer();

            GetRoomUser().Chat("Bien, ese es mi turno de descanso. ¡Nos vemos!", true);
            OnDuty = false;
            GetBotRoleplay().WalkingToItem = false;

            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                var Point = new System.Drawing.Point(Item.GetX, Item.GetY);
                GetRoomUser().MoveTo(Point);
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty)
                return;

            if (GetBotRoleplay().TimerManager.ActiveTimers.ContainsKey("notrabajar"))
                GetBotRoleplay().TimerManager.ActiveTimers["notrabajar"].EndTimer();

            GetBotRoleplay().Invisible = false;
            GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            GetRoomUser().Chat("Bien, hora de volver al trabajo", true);
            OnDuty = true;

            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                var ItemPoint = new System.Drawing.Point(Item.GetX, Item.GetY);
                if (GetRoomUser().Coordinate == ItemPoint)
                {
                    Item.ExtraData = "2";
                    Item.UpdateState(false, true);
                    Item.RequestUpdate(2, true);
                }
            }

            var Point = new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY);
            GetRoomUser().MoveTo(Point);
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }

        public void BeginServingFood(Food.Food Food, GameClient Client)
        {
            if (!OnDuty)
                return;

            if (Client.GetRoleplay() == null)
                return;

            if (Client.GetRoomUser() == null)
                return;

            if (Client.LoggingOut)
                return;

            if (Client.GetRoleplay().CurEnergy >= Client.GetRoleplay().MaxEnergy)
                return;

            if (Client.GetRoleplay().CurAlcohol >= Client.GetRoleplay().MaxAlcohol)
                return;

            string RealName = Food.Name.Substring(0, 1).ToUpper() + Food.Name.Substring(1);

            GetBotRoleplay().WalkingToItem = true;
            GetRoomUser().Chat("En seguida le llevo su pedido a " + Client.GetHabbo().Username + " y le entrega su " + RealName + " ¡En camino!", true);

            var UserPoint = new System.Drawing.Point(Client.GetRoomUser().X, Client.GetRoomUser().Y);
            var ServePoint = new System.Drawing.Point(Client.GetRoomUser().SquareBehind.X, Client.GetRoomUser().SquareBehind.Y);

            object[] Params = { Client, Food, ServePoint, UserPoint, RealName };

            GetRoomUser().MoveTo(ServePoint);
            GetBotRoleplay().TimerManager.CreateTimer("serving", GetBotRoleplay(), 10, true, Params);
        }
    }
}