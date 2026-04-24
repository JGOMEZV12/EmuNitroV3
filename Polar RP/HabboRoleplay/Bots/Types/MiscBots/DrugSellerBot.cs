using System;
using System.Linq;
using System.Text;
using System.Threading;
using Polar.Utilities;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Weapons;
using System.Collections.Generic;
using Polar.Communication.Packets.Outgoing.Rooms.Engine;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.Communication.Packets.Outgoing.Guides;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class DrugSellerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public DrugSellerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {
            
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
            int WeedCost = Convert.ToInt32(RoleplayData.GetData("drugs", "weedcost"));
            int CocaineCost = Convert.ToInt32(RoleplayData.GetData("drugs", "cocainecost"));

            if (Message.ToLower() == Name)
                GetRoomUser().Chat("Hey " + Client.GetHabbo().Username + ", ¿Quieres venderme drogas?", true);
            else if (Message.ToLower() == "cuanto" || Message.ToLower() == "drogas" || Message.ToLower() == "consumo")
            {
                string WhisperMessage = "Actualmente estoy comprando 10g de marihuana por $" + String.Format("{0:N0}", WeedCost) + " y la cocaina por: $" + String.Format("{0:N0}", CocaineCost) + "!";
                Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
            }
            else if (Message.ToLower() == "marihuana")
            {
                if (Client.GetRoleplay().Weed < 10)
                {
                    string WhisperMessage = "No tienes 10g de marihuana para venderme, largate de aquí, ¡Imbecil!";
                    GetRoomUser().Chat("*Le ha dado una puñalada a " + Client.GetHabbo().Username + " por ofrecerle marihuana y no tenerla*", true);
                    Client.GetRoleplay().CurHealth = 0;
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                Client.GetRoleplay().Weed -= 10;
                Client.GetHabbo().Credits += WeedCost;
                Client.GetHabbo().UpdateCreditsBalance();
                GetRoomUser().Chat("*Compra 10g de Marihuana de " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", WeedCost) + "*", true);
            }
            else if (Message.ToLower() == "cocaina")
            {
                if (Client.GetRoleplay().Cocaine < 100)
                {
                    string WhisperMessage = "Tienes que tener minimo 100g de cocaína para venderme";
                    GetRoomUser().Chat("*Le ha dado una puñalada a " + Client.GetHabbo().Username + " por ofrecerle cocaina y no tenerla*", true);
                    Client.GetRoleplay().CurHealth = 0;
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }

                Client.GetRoleplay().Cocaine -= 100;
                Client.GetHabbo().Credits += CocaineCost;
                Client.GetHabbo().UpdateCreditsBalance();
                GetRoomUser().Chat("*Compra 100g de cocaína de " + Client.GetHabbo().Username + " por $" + String.Format("{0:N0}", CocaineCost) + "*", true);
            }
        }

        public override void StopActivities()
        {

        }

        public override void StartActivities()
        {

        }
    }
}