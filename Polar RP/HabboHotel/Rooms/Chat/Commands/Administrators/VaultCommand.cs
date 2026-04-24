using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboHotel.Groups;
using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Administrators
{
    class VaultCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_admin_vault"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Ver/añadir saldo a la boveda."; }
        }

        public async Task Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            Group company = GroupManager.GetJob(9);
            if (company == null)
                return;

            if (Params.Length == 1)
            {
                Session.SendMessage(new BroadcastMessageAlertComposer("La boveda tiene un saldo actual de <b>$"+ company.Balance + "</b>.\r\n Para añadir dinero a la boveda ejecute el comando\r '<b>:boveda add %monto%</b>'"));
                return;
            }
            #endregion

            #region Execute
            if (Params[1] == "add")
            {
                if (Session.GetHabbo().CurrentRoomId != 28)
                {
                    Session.SendWhisper("¡Para usted agregar dinero a la boveda debe estar en la sala del banco (28)!", 1);
                    return;
                }

                int Amount = Convert.ToInt32(Params[2]);
                company.Balance += Amount;
                RoleplayManager.GiveMoneyToCompany(9, Session, "bank", true, Amount);
                /*PolarEnvironment.GetGame().GetRoomManager().UpdateRoom(Room);
                using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.SetQuery("UPDATE `rp_rooms` SET `bank_balance` = @BankBalance WHERE `id` = '" + Room.Id + "' LIMIT 1");
                    dbClient.AddParameter("BankBalance", Convert.ToInt32(Room.BankBalance));
                    dbClient.RunQuery();
                }
                */
                Session.SendWhisper("Se ha actualizado el saldo de la boveda a: $" + company.Balance + ".", 1);
            }
            #endregion
        }
    }
}