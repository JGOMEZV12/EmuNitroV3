using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class OffDutyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_staffduty_off"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Turns your staff duty mode off."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (!Session.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("¡Ya estás fuera de servicio!", 1);
                return;
            }

            Session.GetRoleplay().StaffOnDuty = false;

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect == 102)
                    Session.GetRoomUser().ApplyEffect(0);
            }

            PolarEnvironment.GetGame().GetClientManager().StaffWhisperAlert("¡Acabo de terminar mi turno!", Session);
        }
    }
}
