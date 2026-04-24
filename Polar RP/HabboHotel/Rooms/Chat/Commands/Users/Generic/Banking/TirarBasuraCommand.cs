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
    class TirarBasuraCommand : IChatCommand
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
            get { return "Bota todas la basura que tengas en tu camión."; }
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

            if (Session.GetRoleplay().BasuTrashCount <= 0)
            {
                Session.SendWhisper("No tienes basura para descartar", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("tirarbasura"))
                return;
            #endregion

            #region Execute
            if (!BalanceHide)
            {
                Session.GetRoomUser().ApplyEffect(10);
                Session.GetRoleplay().BasuTrashCount = 0;
                Session.Shout("* Arroja toda la basura al suelo y se ensució de un liquido hediondo [0% Higiene]*", 33);
                Session.GetRoleplay().Hygiene = 0;
                Session.GetRoleplay().CooldownManager.CreateCooldown("tirarbasura", 1000, 10);
            }
            #endregion
        }
    }
}