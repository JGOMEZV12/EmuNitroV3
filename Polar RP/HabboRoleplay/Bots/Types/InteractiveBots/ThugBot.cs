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
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Combat;
using Polar.HabboHotel.Quests;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class ThugBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;

        public ThugBot(int VirtualId)
        {
            this.VirtualId = VirtualId;
            Rand = new CryptoRandom();
        }


        public override void OnDeployed(GameClient Client)
        {
            if (this.GetBotRoleplay().Dead == false)
            {
                if (this.GetBotRoleplay().RoamBot)
                    this.GetBotRoleplay().MoveRandomly();

                if (this.GetBotRoleplay().DRoomUser.Frozen)
                    this.GetBotRoleplay().DRoomUser.Frozen = false;

            }
        }

        public override void OnDeath(GameClient Client)
        {
            int Amount = 0;

            if (Client.GetRoleplay().EquippedWeapon == null)
            {
                Amount = CombatManager.GetCombatType("fist").GetCoins(null, GetBotRoleplay());

                Client.GetHabbo().Credits += Amount;
                Client.GetHabbo().UpdateCreditsBalance();

                CombatManager.GetCombatType("fist").GetRewards(Client, null, GetBotRoleplay());

                if (Amount > 0)
                    RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", noqueandolo y robandole $" + Amount + " de su billetera*", 6);
                else
                    RoleplayManager.Shout(Client, "*Golpea a " + GetBotRoleplay().Name + ", noqueandolo pero no encontró nada en su billetera*", 6);

            }
            else {

                Client.GetHabbo().Credits += Amount;
                Client.GetHabbo().UpdateCreditsBalance();

                CombatManager.GetCombatType("gun").GetRewards(Client, null, GetBotRoleplay());

                if (Amount > 0)
                    RoleplayManager.Shout(Client, "*Mata a " + GetBotRoleplay().Name + " y se queda con $" + Amount + " de su billetera*", 6);
                else
                    RoleplayManager.Shout(Client, "*Mata a " + GetBotRoleplay().Name + "*", 6);

            }
            GetBotRoleplay().InitiateDeath();
        }

        public override void OnArrest(GameClient Client)
        {

        }

        public override void OnAttacked(GameClient Client)
        {

            GetBotRoleplay().UserAttacking = Client;

            if (!GetBotRoleplay().ActiveTimers.ContainsKey("attack"))
            {

                GetBotRoleplay().ActiveTimers.TryAdd("attack", GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id));

                if (GetBotRoleplay().UserAttacking == null)
                    GetRoomUser().Chat("¡Bastardo! te voy a agarrar " + Client.GetHabbo().Username + "!", true, 4);
            }
            else
            {
                if (GetBotRoleplay().ActiveTimers["attack"] == null)
                    GetBotRoleplay().ActiveTimers["attack"] = GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id);
            }

        }

        public override void OnUserLeaveRoom(GameClient Client)
        {

        }

        public override void OnUserEnterRoom(GameClient Client)
        {
            if (Client.GetRoleplay().CombatMode)
            {
                GetBotRoleplay().UserAttacking = Client;

                if (!GetBotRoleplay().ActiveTimers.ContainsKey("attack"))
                {

                    GetBotRoleplay().ActiveTimers.TryAdd("attack", GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id));

                    if (GetBotRoleplay().UserAttacking == null)
                        GetRoomUser().Chat("¡Bastardo! te voy a agarrar " + Client.GetHabbo().Username + "!", true, 4);
                }
                else
                {
                    if (GetBotRoleplay().ActiveTimers["attack"] == null)
                        GetBotRoleplay().ActiveTimers["attack"] = GetBotRoleplay().TimerManager.CreateTimer("attack", GetBotRoleplay(), 10, true, Client.GetHabbo().Id);
                }
            }
        }

        public override void OnUserUseTeleport(GameClient Client, object[] Params)
        {
            if (Client == null || Client.GetRoomUser() == null)
                return;
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
            if(this.GetBotRoleplay().Dead == false)
                this.GetBotRoleplay().MoveRandomly();
        }

        public override void StopActivities()
        {

        }

        public override void OnMessaged(GameClient Client, string Message)
        {

        }

    }
}
