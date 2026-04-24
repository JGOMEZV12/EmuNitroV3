using System;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboRoleplay.Farming;
using Polar.HabboHotel.Rooms.Chat.Commands;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class SupermarketBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public SupermarketBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {
            OnDuty = false;
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

            if (GetBotRoleplay().WalkingToItem)
                return;

            if (RespondToSpeech(Client, Message))
                return;

            string Name = GetBotRoleplay().Name.ToLower();

            string[] Params = Message.Split(' ');

            #region Satchel
            if (Message.ToLower() == "satchel")
            {
                string WhisperMessage = "para comprar diga 'bolsa semillas' para comprar una bolsa para semillas y 'plant satchel' para comprar una bolsa para plantas";
                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                return;
            }
            #endregion

            #region Plant Satchel
            if (Message.StartsWith("bolsa vegetal") || Message.StartsWith("plant satchel"))
            {
                if (Client.GetRoleplay().FarmingStats.HasPlantSatchel)
                {
                    string WhisperMessage = "Ya tienes bolsa vegetal";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                if (Client.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => x.Type.ToLower() == "bolsavegetal").ToList().Count > 0)
                {
                    string WhisperMessage = "Lo sentimos, pero ya se le ha ofrecido una bolsa vegetal";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                int Cost = Convert.ToInt32(RoleplayData.GetData("farming", "plantsatchelcost"));

                if (Client.GetHabbo().Credits < Cost)
                {
                    string WhisperMessage = "Lo siento, no tienes $" + String.Format("{0:N0}", Cost) + " Para comprar una bolsa vegetal";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                GetRoomUser().Chat("*Ofrece una bolsa vegetal a " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                Client.GetRoleplay().OfferManager.CreateOffer("bolsavegetal", 0, Cost, this);
                Client.SendWhisper("Recientemente se le ha ofrecido una bolsa vegetal por $" + String.Format("{0:N0}", Cost) + " escriba ':aceptar bolsavegetal' para ¡comprarlo!", 1);
                return;
            }
            #endregion

            #region Seed Satchel
            if (Message.StartsWith("bolsa semillas"))
            {
                if (Client.GetRoleplay().FarmingStats.HasSeedSatchel)
                {
                    string WhisperMessage = "Ya tienes bolsa para semillas";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                if (Client.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => x.Type.ToLower() == "bolsasemillas").ToList().Count > 0)
                {
                    string WhisperMessage = "Lo sentimos, pero ya se le ha ofrecido una bolsa para semillas";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                int Cost = Convert.ToInt32(RoleplayData.GetData("farming", "seedsatchelcost"));

                if (Client.GetHabbo().Credits < Cost)
                {
                    string WhisperMessage = "No tienes $" + String.Format("{0:N0}", Cost) + " para comprar esto";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                GetRoomUser().Chat("*Ofrece una bolsa para semillas a " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                Client.GetRoleplay().OfferManager.CreateOffer("bolsasemillas", 0, Cost, this);
                Client.SendWhisper("Se le ha ofrecido una bolsa de semillas por $" + String.Format("{0:N0}", Cost) + "! diga ':aceptar bolsasemillas' para comprar", 1);
                return;
            }
            #endregion

            #region Seeds
            if (Message.ToLower() == "seeds" || Message.ToLower() == "seed" || Message.ToLower() == "semillas")
            {
                GetRoomUser().Chat("Bienvenido " + Client.GetHabbo().Username + " al supermercado", true);

                StringBuilder FarmingList = new StringBuilder().Append("--- EN VENTA ---\n");
                FarmingList.Append("Para comprar 'semillas <id> <cantidad>'!\n Si desea comprar una bolsa para sus semillas diga: Bolsa Semillas\n\n");

                foreach (FarmingItem Item in FarmingManager.FarmingItems.Values.OrderBy(x => x.Id))
                {
                    if (Item != null)
                    {
                        ItemData Furni;
                        if (PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                        {
                            FarmingList.Append("*" + Furni.PublicName + " [" + Item.Id + "]*\n");
                            FarmingList.Append("REQUIERE NIVEL DE AGRICULTURA: " + Item.LevelRequired + "\n");
                            FarmingList.Append("PRECIO: $" + String.Format("{0:N0}", Item.BuyPrice) + "\n\n");
                        }
                    }
                }

                Client.SendMessage(new MOTDNotificationComposer(FarmingList.ToString()));
                return;
            }
            #endregion

            #region Buying Seeds
            if (Params[0].ToLower() == "semillas" && Params.Length > 2)
            {
                int Id;
                if (!int.TryParse(Params[1], out Id))
                {
                    string WhisperMessage = "Por favor escriba 'semillas <id> <amount>' para comprar algunas semillas, diga 'semillas' Para ver lo que tengo a la venta";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                FarmingItem Item = FarmingManager.GetFarmingItem(Id);

                ItemData Furni;
                if (!PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni) || Item == null)
                {
                    string WhisperMessage = "Lo sentimos, pero no hay semilla para la venta con ese ID";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                int Amount;
                if (!int.TryParse(Params[2], out Amount))
                {
                    string WhisperMessage = "Porfavor escriba 'semillas <id> <cantidad>' Para comprar algunas semillas, o tipo 'semillas' para ver lo que tengo a la venta!";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                if (Client.GetRoleplay().OfferManager.ActiveOffers.Values.Where(x => x.Type.ToLower() == "semillas").ToList().Count > 0)
                {
                    string WhisperMessage = "Lo siento, pero ya le han ofrecido algunas semillas";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                if (!Client.GetRoleplay().FarmingStats.HasSeedSatchel)
                {
                    string WhisperMessage = "Usted no tiene una bolsa de semillas para llevar las semillas! Tipo 'bolsa semillas' para comprar uno.";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                if (Item.LevelRequired > Client.GetRoleplay().FarmingStats.Level)
                {
                    string WhisperMessage = "Lo siento, pero no tiene un nivel de cultivo lo suficientemente alto para este tipo de semilla!";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                int Cost = (Amount * Item.BuyPrice);
                if (Client.GetHabbo().Credits < Cost)
                {
                    string WhisperMessage = "Tu no tienes $" + String.Format("{0:N0}", Cost) + " para comprar " + Amount + " " + Furni.PublicName + "'s!";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                object[] Objects = new object[] { this, Item };
                GetRoomUser().Chat("*Ofrece " + Amount + " " + Furni.PublicName + " semillas a " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                Client.GetRoleplay().OfferManager.CreateOffer("semillas", 0, Amount, Objects);
                Client.SendWhisper("Acabas de ofrecerte " + Amount + " " + Furni.PublicName + " semillas por $" + String.Format("{0:N0}", Cost) + " diga ':aceptar semillas' Para comprarlo", 1);
                return;
            }
            #endregion
        }

        public override void StopActivities()
        {
            if (!OnDuty)
                return;

            EndTimerSafe("trabajar");

            GetRoomUser().Chat("Hora de irme a casa, ya termina mi turno", true);
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

            EndTimerSafe("notrabajar");

            GetBotRoleplay().Invisible = false;
            GetRoom().SendMessage(new UsersComposer(GetRoomUser()));

            if (GetRoomUser() == null)
                return;

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
    }
}