using System;
using System.Linq;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Pathfinding;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class HospitalBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public HospitalBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

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
                RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", Derribándolo y robando $" + Amount + " de su cartera*", 6);
            else
                RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", para robarlo, pero su billetera esta vacia*", 6);

            GetBotRoleplay().InitiateDeath();
        }

        public override void OnArrest(GameClient Client) { }

        // FIX Bug 4: OnAttacked simplificado
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
            Client.SendWhisper("Hey, ¿Necesitas ayuda? di 'Curame' para que el doctor pueda hacer algo por ti", 1);
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
            if (RespondToSpeech(Client, Message)) return;

            string Name = GetBotRoleplay().Name.ToLower();
            string[] keys = { "heal", "aid", "heal me", "heal please", "med aid", "curame", "Curame" };
            string sKeyResult = keys.FirstOrDefault(s => Message.Contains(s));
            if (sKeyResult == null) return;

            if (Message.ToLower() == Name)
            {
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Necesitas ayuda?", true);
                return;
            }

            switch (sKeyResult.ToLower())
            {
                case "heal":
                case "curame":
                {
                    if (Client.GetRoleplay().IsDead)
                    {
                        if (Client.GetRoleplay().DeadTimeLeft <= 1 || Client.GetRoleplay().BeingHealed)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Ya vas a ser dado de alta pronto! No hay necesidad de mi ayuda.", 0, 2));
                            break;
                        }
                        if (!GetBotRoleplay().WalkingToItem)
                            InitiateDischarge(Client);
                        else
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "Ya estoy en mi camino para ayudar a alguien! Por favor, espere hasta que esté libre.", 0, 2));
                    }
                    else
                    {
                        if (Client.GetRoleplay().CurHealth >= Client.GetRoleplay().MaxHealth)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Ya estás completamente curado!", 0, 2));
                            break;
                        }
                        if (Client.GetRoleplay().BeingHealed)
                        {
                            Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, "¡Ya estás sanado!", 0, 2));
                            break;
                        }
                        GetRoomUser().Chat("Espero que te sientas mejor pronto " + Client.GetHabbo().Username + " también te he Inyectado un complejo de Vitamina B12!¨(-100$)", true);
                        Client.GetRoleplay().BeingHealed = true;
                        Client.GetRoleplay().CurEnergy = Client.GetRoleplay().MaxEnergy;
                        Client.GetHabbo().Credits -= 200;
                        Client.GetHabbo().UpdateCreditsBalance();
                        Client.GetRoleplay().TimerManager.CreateTimer("heal", 1000, false);
                    }
                    break;
                }
            }
        }

        public override void StopActivities()
        {
            if (!OnDuty) return;
            // FIX Bug 1: EndTimerSafe
            EndTimerSafe("trabajar");
            EndTimerSafe("discharge");
            GetRoomUser().Chat("Bien, he culminado mi turno. ¡Nos vemos!", true);
            OnDuty = false;
            GetBotRoleplay().WalkingToItem = false;
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

        public void InitiateDischarge(GameClient Client)
        {
            if (!OnDuty || Client?.GetRoleplay() == null || Client.GetRoomUser() == null || Client.LoggingOut || !Client.GetRoleplay().IsDead)
                return;

            GetBotRoleplay().WalkingToItem = true;
            GetRoomUser().Chat("voy en camino " + Client.GetHabbo().Username + "!", true);

            var UserPoint = new System.Drawing.Point(Client.GetRoomUser().X, Client.GetRoomUser().Y);
            var Items = GetRoom().GetGameMap().GetAllRoomItemForSquare(UserPoint.X, UserPoint.Y);
            Item Item = Items.FirstOrDefault(x => x.GetBaseItem().ItemName == "hosptl_bed");

            if (Item == null) return;

            var GoToPoint = new System.Drawing.Point(Item.SquareLeft.X, Item.SquareLeft.Y);
            GetRoomUser().MoveTo(GoToPoint);
            GetBotRoleplay().TimerManager.CreateTimer("discharge", GetBotRoleplay(), 10, true, Client);
        }
    }
}