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
    class StopTaxiCommand : IChatCommand
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
            get { return "Detiene la llamada para el taxi si cambias de opinión."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetRoleplay().InsideTaxi)
            {
                Session.SendWhisper("¡No estás dentro de un taxi!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("stoptaxi", true))
                return;

            bool IsVip = Session.GetHabbo().VIPRank < 1 ? false : true;
            string TaxiText = IsVip ? " VIP" : "";

            Session.Shout("*Cancela su Taxi " + TaxiText + " antes de que llegue y el taxista se molesta*", 4);
            Session.GetRoleplay().InsideTaxi = false;
            Session.GetRoomUser().ApplyEffect(0);
            Session.GetRoleplay().CooldownManager.CreateCooldown("stoptaxi", 1000, 5);
        }
    }
}