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
using Polar.HabboRoleplay.Farming;

namespace Polar.HabboRoleplay.Bots.Types
{
    public class PlantSellerBot : RoleplayBotAI
    {
        int VirtualId;
        CryptoRandom Rand;
        public bool CheckForOtherWorkers;
        public int OnDutyCheckInterval;
        public int CurOnDutyCheckTime;

        public PlantSellerBot(int VirtualId)
        {
            this.OnDuty = true;
            this.CheckForOtherWorkers = true;
            this.CurOnDutyCheckTime = 0;
            this.VirtualId = VirtualId;

            Rand = new CryptoRandom();
        }

        public override void OnDeployed(GameClient Client)
        {

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

            if (Message.ToLower() == Name)
                GetRoomUser().Chat("Hola " + Client.GetHabbo().Username + ", ¿Quieres vender algunas plantas?", true);
            else if (Message.ToLower() == "si" || Message.ToLower() == "plant" || Message.ToLower() == "plantas" || Message.ToLower() == "sell")
            {
                StringBuilder Plants = new StringBuilder().Append("----- Tasa del día -----\n");
                Plants.Append("diga 'vender plantas' para venderme todas sus plantas!\n\n");

                foreach (var Item in FarmingManager.FarmingItems.Values)
                {
                    if (Item == null)
                        continue;

                    ItemData Furni;
                    if (PolarEnvironment.GetGame().GetItemManager().GetItem(Item.BaseItem, out Furni))
                    {
                        Plants.Append("--- " + Furni.PublicName + " ---\n");
                        Plants.Append("Venta Beneficio: " + String.Format("{0:N0}", Item.SellPrice) + " por planta\n\n");
                    }
                }

                GetRoomUser().Chat("Mira " + Client.GetHabbo().Username + ", Aquí la lista de plantas que estoy comprando actualmente,", true);
                Client.SendMessage(new MOTDNotificationComposer(Plants.ToString()));
            }
            else if (Message.ToLower() == "vender plantas")
            {
                int Amount = FarmingManager.SellPlants(Client);

                if (Amount > 0)
                {
                    Client.GetHabbo().Credits += Amount;
                    Client.GetHabbo().UpdateCreditsBalance();
                    RoleplayManager.Shout(Client, "*Vende todas sus plantas a " + this.GetBotRoleplay().Name + "*", 4);
                    Client.SendWhisper("Acabas de ganar $" + String.Format("{0:N0}", Amount) + " por vender tus plantas", 1);
                    GetRoomUser().Chat("Gracias por venderme tus plantas " + Client.GetHabbo().Username + " las usaremos para crear comidas y medicinas", true);
                    return;
                }
                else
                {
                    string WhisperMessage = "No tienes plantas para vender " + Client.GetHabbo().Username + " cultiva un poco y vendeme ¡Las necesito!";
                    Client.SendMessage(new WhisperComposer(GetRoomUser().VirtualId, WhisperMessage, 0, 2));
                    return;
                }
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