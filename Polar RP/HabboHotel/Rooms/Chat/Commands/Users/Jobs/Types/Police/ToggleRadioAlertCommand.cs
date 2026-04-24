using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class ToggleRadioAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_toggle_radio_alerts"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite ignorar las alertas VIP."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Group Job = GroupManager.GetJob(Session.GetRoleplay().JobId);

            if (Job == null)
            {
                Session.SendWhisper("¡Estás desempleado!", 1);
                return;
            }

            if (Job.Id <= 0)
            {
                Session.SendWhisper("¡Estás desempleado!", 1);
                return;
            }

            if (!GroupManager.HasJobCommand(Session, "radio") && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("¡Sólo los oficiales de policía pueden usar este comando!", 1);
                return;
            }

            Session.GetRoleplay().DisableRadio = !Session.GetRoleplay().DisableRadio;
            Session.SendWhisper("Usted es " + (Session.GetRoleplay().DisableRadio ? "now" : "no longer") + " Ignorando las alertas de radio", 1);
        }
    }
}
