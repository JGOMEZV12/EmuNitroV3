using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboRoleplay.Combat;
using Polar.HabboRoleplay.Weapons;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Combat
{
    class CombatModeCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_combat_mode"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Activa el modo de combate (haciendo clic en un usuario los ataca)."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            if (Session.GetRoleplay().IsNoob == true)
            {
                Session.SendWhisper("¡Debes esperar que termine la inmunidad!  >:)", 1);
                return;
            }

            Session.GetRoleplay().CombatMode = !Session.GetRoleplay().CombatMode;
            Session.GetRoleplay().InCombat = false;
            Session.SendWhisper("Modo de combate ahora " + (Session.GetRoleplay().CombatMode == true ? "activo" : "desactivado"), 1);
            return;
        }
    }
}