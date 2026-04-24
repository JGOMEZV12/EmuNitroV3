using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class BackupCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_backup"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Solicitudes de respaldo / ayuda a todos los oficiales de policía en línea."; }
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

            if (!Session.GetRoleplay().IsWorking)
            {
                Session.SendWhisper("¡Debes estar trabajando para usar este comando!", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("police_backup"))
                return;

            PolarEnvironment.GetGame().GetClientManager().JailAlert(Session.GetHabbo().Username + " Está solicitando apoyo " + Room.Name + " (ID: " + Room.Id + "). ¡VE RÁPIDO ALLÍ!");
            Session.GetRoleplay().CooldownManager.CreateCooldown("police_backup", 1000, 5);
        }
    }
}
