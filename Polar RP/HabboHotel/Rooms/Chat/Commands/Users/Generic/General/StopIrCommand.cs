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
    class StopIrCommand : IChatCommand
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
            get { return "Detiene el carro si cambias de opinión."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetRoleplay().InsideTaxi)
            {
                Session.SendWhisper("¡No estás dentro de un carro", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("stoptaxi", true))
                return;

            bool IsVip = Session.GetHabbo().VIPRank < 1 ? false : true;
            string TaxiText = IsVip ? "VIP" : "";

            Session.Shout("*Detiene el vehiculo y lo apaga*", 4);
            Session.GetRoleplay().InsideTaxi = false;
            Session.GetRoleplay().DrivingCar = false;
            Session.GetRoleplay().CooldownManager.CreateCooldown("stoptaxi", 1000, 5);
        }
    }
}