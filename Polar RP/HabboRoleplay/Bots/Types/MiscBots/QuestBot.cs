using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms;
using Polar.HabboRoleplay.Bots.Types;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.Utilities;
using System.Threading;
using Polar.Communication.Packets.Outgoing.Messenger;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Quests;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class QuestBot : RoleplayBotAI
    {

        int VirtualId;
        CryptoRandom Rand;

        public QuestBot(int VirtualId)
        {
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {

        }

        public override void OnDeath(GameClient Client)
        {
            int Amount = CombatManager.GetCombatType("fist").GetCoins(null, GetBotRoleplay());

            Client.GetHabbo().Credits += Amount;
            Client.GetHabbo().UpdateCreditsBalance();

            CombatManager.GetCombatType("fist").GetRewards(Client, null, GetBotRoleplay());

            if (Amount > 0)
                RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", Derribándolos y robando $" + Amount + " de su cartea*", 6);
            else
                RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", buscando su billetera*", 6);


            GetBotRoleplay().InitiateDeath();
        }

        public override void OnArrest(GameClient Client)
        {

        }

        // FIX Bug 4: OnAttacked simplificado — CreateTimer ya maneja deduplicación
        public override void OnAttacked(GameClient Client)
        {
            GetBotRoleplay().UserAttacking = Client;
            GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id);
            if (GetBotRoleplay().UserAttacking == null)
                GetRoomUser().Chat("Bastarto, te voy agarrar y te daré una paliza " + Client.GetHabbo().Username + "!", true, 4);
        }

        public override void OnUserLeaveRoom(GameClient Client)
        {

        }

        public override void OnUserEnterRoom(GameClient Client)
        {

        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {

        }

        public override void OnUserSay(RoomUser User, string Message)
        {
            HandleRequest(User.GetClient(), Message);
        }

        public override void OnUserShout(RoomUser User, string Message)
        {
            HandleRequest(User.GetClient(), Message);
        }

        public override void HandleRequest(GameClient Client, string Message)
        {
            if (RespondToSpeech(Client, Message))
                return;
        }

        public override void StartActivities()
        {

        }

        public override void StopActivities()
        {

        }

        public override void OnMessaged(GameClient Client, string Message)
        {
            List<string> Replies = new List<string>();
            Replies.Add("¿Qué deseas?");
            Replies.Add("¿Puede dejar de mensajearme?");
            Replies.Add("Estoy bastante ocupado, hablemos más tarde.");
            Replies.Add("Amigo, vete a la mierda, Gracias.");
            Replies.Add("Uh....");

            string Reply = Replies[new CryptoRandom().Next(0, Replies.Count - 1)];
            int ReplyTime = new CryptoRandom().Next(3000, 7000);

            new Thread(() =>
            {
                Thread.Sleep(ReplyTime);
                GetBotRoleplay().MessageFriend(Client.GetHabbo().Id, Reply);
            }).Start();
        }

    }
}