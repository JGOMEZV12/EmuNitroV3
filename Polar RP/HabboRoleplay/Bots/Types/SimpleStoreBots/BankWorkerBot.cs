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
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboHotel.Quests;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class BankWorkerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public BankWorkerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

        // FIX Bug 8: OnDeployed estaba vacío — el bot nunca iniciaba actividades
        public override void OnDeployed(GameClient Client) => this.StartActivities();

        public override void OnDeath(GameClient Client)
        {
            int Amount = CombatManager.GetCombatType("fist").GetCoins(null, GetBotRoleplay());
            Client.GetHabbo().Credits += Amount;
            Client.GetHabbo().UpdateCreditsBalance();
            PolarEnvironment.GetGame().GetQuestManager().ProgressUserQuest(Client, QuestType.KILL_USER);
            PolarEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Client, "ACH_Kills", 1);
            Client.GetRoleplay().Kills++;
            Client.GetRoleplay().HitKills++;

            CryptoRandom Random = new CryptoRandom();
            int Multiplier = 1;
            int Chance = Random.Next(1, 101);
            if (Chance <= 16) Multiplier = Chance <= 8 ? 3 : 2;

            LevelManager.AddLevelEXP(Client, CombatManager.GetCombatType("fist").GetEXP(Client, null, GetBotRoleplay()) * Multiplier);

            if (Amount > 0)
                RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", Derribándolo y robando $" + Amount + " De su cartera*", 6);
            else
                RoleplayManager.Shout(Client, "*Golpea  " + GetBotRoleplay().Name + ", Tirandolo al suelo pero su billetera vacía*", 6);

            GetBotRoleplay().InitiateDeath();
        }

        public override void OnArrest(GameClient Client) { }

        // FIX Bug 4: OnAttacked simplificado — CreateTimer ya hace TryAdd internamente
        public override void OnAttacked(GameClient Client)
        {
            GetBotRoleplay().UserAttacking = Client;
            GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id);
            if (GetBotRoleplay().UserAttacking == null)
                GetRoomUser().Chat("¡Bastardo! te voy a agarrar " + Client.GetHabbo().Username + "!", true, 4);
        }

        public override void OnUserLeaveRoom(GameClient Client) { if (!OnDuty) return; }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (!OnDuty) return;
            if (Client.GetRoleplay().BankAccount == 0)
                Client.SendWhisper("Para aperturar tu cuenta bancaria, toma asiento y di 'corriente' o 'ahorro' dependiendo que tipo de cuenta deseas escoger.", 1);
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (!OnDuty || Client == null || Client.GetRoomUser() == null) return;
            if (Client == GetBotRoleplay().UserFollowing || Client == GetBotRoleplay().UserAttacking)
                GetBotRoleplay().StartTeleporting(GetRoomUser(), GetRoom(), Params);
        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            if (!OnDuty) return;
            var Client = User.GetClient();
            if (Client == null) return;
            HandleRequest(Client, Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            if (!OnDuty || User.GetClient() == null) return;
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnMessaged(GameClient Client, string Message) { if (!OnDuty) return; }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (!OnDuty) return;
            if (GetBotRoleplay().WalkingToItem) return;

            string Name = GetBotRoleplay().Name.ToLower();
            if (RespondToSpeech(Client, Message)) return;

            if (Message.ToLower() == Name)
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas algo?", true);
            else
                switch (Message.ToLower())
                {
                    case "chequings":
                    case "corriente":
                    {
                        if (Client.GetRoleplay().BankAccount > 0)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Ya tienes una cuenta corriente!", 0, 2));
                            break;
                        }
                        bool HasOffer = Client.GetRoleplay().OfferManager.ActiveOffers.Values
                            .Any(o => o.Type.ToLower() == "corriente");
                        if (!HasOffer)
                        {
                            GetRoomUser().Chat("*Ofrece abrir una cuenta corriente a " + Client.GetHabbo().Username + " ¡Gratis!*", true);
                            Client.GetRoleplay().OfferManager.CreateOffer("corriente", 0, 0, this);
                            Client.SendWhisper("Acaba de recibir una Cuenta Corriente gratis, Escribe ':aceptar corriente' para activarla", 1);
                        }
                        else
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Ya se le ha ofrecido una cuenta corriente", 0, 2));
                        break;
                    }

                    case "savings":
                    case "ahorro":
                    {
                        int Cost = 2500;
                        if (Client.GetRoleplay().BankAccount > 1)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Ya tiene una cuenta de ahorros!", 0, 2));
                            break;
                        }
                        if (Client.GetHabbo().Credits < Cost)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "No puedes pagar una Cuenta de Ahorros, cuestan $" + String.Format("{0:N0}", Cost) + "!", 0, 2));
                            break;
                        }
                        bool HasOffer = Client.GetRoleplay().OfferManager.ActiveOffers.Values
                            .Any(o => o.Type.ToLower() == Message.ToLower());
                        if (!HasOffer)
                        {
                            GetRoomUser().Chat("*Ofrece una cuenta de ahorro a " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", Cost) + "*", true);
                            Client.GetRoleplay().OfferManager.CreateOffer("ahorro", 0, Cost, this);
                            Client.SendWhisper("Se le ha ofrecido una cuenta de ahorros por $" + String.Format("{0:N0}", Cost) + "! Type ':aceptar ahorro' para activarla", 1);
                        }
                        else
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Ya se le ha ofrecido una cuenta de ahorros", 0, 2));
                        break;
                    }
                }
        }

        public override void StopActivities()
        {
            if (!OnDuty) return;
            // FIX Bug 1: usar EndTimerSafe en lugar de acceso directo al diccionario
            EndTimerSafe("trabajar");
            GetRoomUser().Chat("Bien, termina mi turno. ¡Nos vemos!", true);
            OnDuty = false;
            if (GetBotRoleplay().WorkUniform != "none")
                GetRoom().SendMessage(new UsersComposer(GetRoomUser()));
            Item Item;
            if (GetBotRoleplay().GetStopWorkItem(this.GetRoom(), out Item))
            {
                GetRoomUser().MoveTo(new System.Drawing.Point(Item.GetX, Item.GetY));
                GetBotRoleplay().TimerManager.CreateTimer("notrabajar", GetBotRoleplay(), 10, true, null);
            }
        }

        public override void StartActivities()
        {
            if (OnDuty) return;
            EndTimerSafe("notrabajar");
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
            GetRoomUser().MoveTo(new System.Drawing.Point(GetBotRoleplay().oX, GetBotRoleplay().oY));
            GetBotRoleplay().TimerManager.CreateTimer("trabajar", GetBotRoleplay(), 10, true, null);
        }
    }
}