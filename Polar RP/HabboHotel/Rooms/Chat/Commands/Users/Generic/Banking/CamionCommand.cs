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
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Banking
{
    class CamionCommand : IChatCommand
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
            get { return "Le dice la cantidad de basura en el camión."; }
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

            if (!GroupManager.HasJobCommand(Session, "recolector"))
            {
                Session.SendWhisper("No tienes camión para ver la capacidad", 1);
                return;
            }

            if (Session.GetRoleplay().DrivingCar == false)
            {
                Session.SendWhisper("Maneja tu camión de basura para revisar cuanta carga tienes", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("camion"))
                return;
            #endregion

            #region Execute
            if (!BalanceHide)
            {
                Session.Shout("*Revisa la capacidad del camión: " + String.Format("{0:N0}", Session.GetRoleplay().BasuTrashCount) + "/15*", 33);
                Session.GetRoleplay().CooldownManager.CreateCooldown("camion", 1000, 5);
            }
            #endregion
        }
    }
}