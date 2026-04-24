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

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Banking
{
    class CedulaCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_banking_balance"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Aprenda su DNI/Cedula de identidad."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            bool BalanceHide = false;
            if (Params.Length > 1)
            {
                if (Params[1].ToLower() == "hide")
                    BalanceHide = true;
            }

            if (Session.GetRoleplay().TryGetCooldown("cedula"))
                return;
            #endregion

            #region Execute
            if (!BalanceHide)
            {
                Session.SendWhisper("*DNI/CÉDULA:   #" + String.Format("{0:N0}", Session.GetHabbo().Id) + "   [No compartas tu DNI con nadie]*", 33);
                Session.GetRoleplay().CooldownManager.CreateCooldown("cedula", 1000, 5);
            }
            #endregion
        }
    }
}