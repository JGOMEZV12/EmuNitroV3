using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class RadioAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_alert_radio"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Envía un mensaje por radio a todos los oficiales de policía en línea."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un mensaje para enviar.", 1);
                return;
            }

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

            if (Session.GetRoleplay().DisableRadio)
            {
                Session.SendWhisper("¡Tiene alertas de radio deshabilitadas! Tipo ': togglera' para volver a habilitarlos!", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            if (Session.GetHabbo().Translating)
            {
                string LG1 = Session.GetHabbo().FromLanguage.ToLower();
                string LG2 = Session.GetHabbo().ToLanguage.ToLower();

                PolarEnvironment.GetGame().GetClientManager().RadioAlert(PolarEnvironment.TranslateText(Message, LG1 + "|" + LG2) + " [" + LG1.ToUpper() + " -> " + LG2.ToUpper() + "]", Session);
            }
            else
                PolarEnvironment.GetGame().GetClientManager().RadioAlert(Message, Session);
        }
    }
}
