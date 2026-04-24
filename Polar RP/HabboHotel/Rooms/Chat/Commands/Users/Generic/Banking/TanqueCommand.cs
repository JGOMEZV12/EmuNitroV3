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
    class TanqueCommand : IChatCommand
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
            get { return "Le dice la cantidad de gasolina que tiene."; }
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

            if (Session.GetRoleplay().CarFuel <= 0)
            {
                Session.SendWhisper("¡No tiene gasolina en el tanque! vaya en taxi a la 12 y escriba: 'Gasolina cantidad'.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("tanque"))
                return;
            #endregion

            #region Execute
            if (!BalanceHide)
            {
                Session.GetRoomUser().ApplyEffect(65);
                Session.SendWhisper("*Saca su teléfono y abre AppCar y tiene: " + String.Format("{0:N0}", Session.GetRoleplay().CarFuel) + " de gasolina*", 33);
                Session.GetRoleplay().CooldownManager.CreateCooldown("tanque", 1000, 5);
            }
            #endregion
        }
    }
}