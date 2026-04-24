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
    class BalanceCommand : IChatCommand
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
            get { return "Le dice su saldo bancario."; }
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
           
            if (Session.GetRoleplay().BankAccount <= 0)
            {
                Session.SendWhisper("¡No tienes cuentas bancarias! Pida a un trabajador bancario que abra una cuenta para usted.", 1);
                return;
            }

            if (Room.BankEnabled == false && Session.GetRoleplay().Phone == 0)
            {
                Session.SendWhisper("¡No dispone de un teléfono para verificar su cuenta bancaria de forma remota! Por favor vaya al banco si quiere revisar su saldo.", 1);
                return;
            }

            if (Session.GetRoleplay().TryGetCooldown("balance"))
                return;
            #endregion

            #region Execute
            if (!BalanceHide)
            {
                Session.GetRoomUser().ApplyEffect(65);
                Session.SendWhisper("*Saca su teléfono y abre aplicación de banco para ver su saldo y tiene: $" + String.Format("{0:N0}", Session.GetRoleplay().BankChequings) + "*", 33);
                Session.GetRoleplay().CooldownManager.CreateCooldown("balance", 1000, 5);
                Session.GetRoomUser().ApplyEffect(0);
            }
            else
            {
                Session.Shout("*Comprueba su saldo bancario*", 5);
                Session.GetRoomUser().ApplyEffect(65);
                Session.SendWhisper("Tienes $" + String.Format("{0:N0}", Session.GetRoleplay().BankChequings) + " En su corriente.", 33);
                Session.GetRoleplay().CooldownManager.CreateCooldown("balance", 1000, 5);
                Session.GetRoomUser().ApplyEffect(0);
            }

            if (Session.GetRoleplay().BankAccount > 1)
                Session.GetRoomUser().ApplyEffect(65);
            Session.SendWhisper("También tienes $" + String.Format("{0:N0}", Session.GetRoleplay().BankSavings) + " En su cuenta de ahorro.", 33);
            Session.GetRoomUser().ApplyEffect(0);
            #endregion
        }
    }
}