using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.Communication.Packets.Outgoing.Navigator;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class StopBusCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_taxi_stop"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Cancela subirte al bus."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetRoleplay().InsideBus)
            {
                Session.SendWhisper("¡No estás dentro de un Bus!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("stoptaxi", true))
                return;

            bool IsVip = Session.GetHabbo().VIPRank > 0 ? true : false;
            string TaxiText = IsVip ? "VIP" : "";

            Session.Shout("*Se baja del BUS " + TaxiText + " antes de que arranque*", 4);
            Session.GetRoleplay().InsideBus = false;
            Session.GetRoleplay().CooldownManager.CreateCooldown("stoptaxi", 1000, 5);
        }
    }
}