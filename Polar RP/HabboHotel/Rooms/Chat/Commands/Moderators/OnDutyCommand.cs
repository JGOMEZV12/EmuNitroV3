using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class OnDutyCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_staffduty_on"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Trabaja como staff."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetRoleplay().StaffOnDuty)
            {
                Session.SendWhisper("Trabajando como staff", 1);
                return;
            }

            if (Session.GetRoleplay().IsWorkingOut)
            {
                Session.SendWhisper("Deja el rol para poder trabajar como staff", 1);
                return;
            }

            Session.GetRoleplay().StaffOnDuty = true;
            Session.GetRoleplay().IsWorking = false;

            if (Session.GetRoomUser() != null)
            {
                if (Session.GetRoomUser().CurrentEffect != 102)
                    Session.GetRoomUser().ApplyEffect(102);
            }

            PolarEnvironment.GetGame().GetClientManager().StaffWhisperAlert("Tiene activo :strabajar", Session);
        }
    }
}
