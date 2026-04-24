using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboHotel.Subscriptions;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class FastwalkCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_fast_walk"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te da la habilidad de correr."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            SubscriptionData SubData = null;
            if (!PolarEnvironment.GetGame().GetSubscriptionManager().TryGetSubscriptionData(Session.GetHabbo().VIPRank, out SubData) || Session.GetHabbo().VIPRank <= 0)
            {
                Session.SendWhisper("¡No eres VIP!", 1);
                return;
            }

            RoomUser User = Session.GetRoomUser();
            if (User == null)
                return;

            User.FastWalking = !User.FastWalking;

            if (User.SuperFastWalking)
                User.SuperFastWalking = false;

            Session.SendWhisper("Modo de caminata actualizado.", 1);
        }
    }
}
