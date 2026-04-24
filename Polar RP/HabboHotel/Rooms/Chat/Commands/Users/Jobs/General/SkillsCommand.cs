using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Jobs.General
{
    class SkillsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_skills"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Te muestra tu progreso para trabajos secundarios."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Session.GetRoleplay().TryGetCooldown("skills"))
                return;
            #endregion

            #region Execute
            string str = "";
            str += "\n============================================\n                  Habilidades de Trabajos \n============================================\n";

            str += "Trabajo de Camionero (Nivel "+ Session.GetRoleplay().CamLvl +": - Progreso "+ Session.GetRoleplay().CamXP + "/50)\n";
            /*str += "Trabajo de Armero (Nivel "+ Session.GetRoleplay().ArmLvl +": - Progreso "+ Session.GetRoleplay().ArmXP + "/50)\n";
            str += "Trabajo de Mecánico (Nivel "+ Session.GetRoleplay().MecLvl +": - Progreso "+ Session.GetRoleplay().MecXP + "/50)\n";
            str += "Trabajo de Basurero (Nivel "+ Session.GetRoleplay().BasuLvl +": - Progreso "+ Session.GetRoleplay().BasuXP + "/50)\n";
            str += "Trabajo de Ladrón (Nivel "+ Session.GetRoleplay().LadronLvl +": - Progreso "+ Session.GetRoleplay().LadronXP + "/50)\n";
            str += "Trabajo de Minero (Nivel "+ Session.GetRoleplay().MinerLvl +": - Progreso "+ Session.GetRoleplay().MinerXP + "/50)\n";*/

            Session.SendNotifWithScroll(str);
            //Session.SendMessage(new MOTDNotificationComposer(str));
            Session.GetRoleplay().CooldownManager.CreateCooldown("skills", 1000, 3);
            #endregion
        }
    }
}