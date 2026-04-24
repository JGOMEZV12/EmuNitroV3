using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Purchasing
{
    class BuyTicketCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_purchasing_buy_ticket"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite comprar un boleto de lotería."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Variables
            int Cost = LotteryManager.Cost;
            int LotteryRoom = Convert.ToInt32(RoleplayData.GetData("lottery", "room"));
            #endregion

            #region Conditions
            if (Room.Id != LotteryRoom)
            {
                Session.SendWhisper("Usted debe estar en la tienda de lotería para comprar un boleto", 1);
                return;
            }

            if (Session.GetHabbo().Credits < Cost)
            {
                Session.SendWhisper("¡No tienes suficiente dinero para comprar un billete!", 1);
                return;
            }

            if (LotteryManager.LotteryTickets.ContainsKey(Session.GetHabbo().Id))
            {
                Session.SendWhisper("¡Ya has comprado un boleto para esta lotería!", 1);
                return;
            }

            if (LotteryManager.LotteryFull())
            {
                Session.SendWhisper("Lo sentimos, pero no hay entradas para la venta", 1);
                return;
            }
            #endregion

            #region Execute
            int TicketId = LotteryManager.LotteryTickets.Count + 1;

            LotteryManager.LotteryTickets.TryAdd(Session.GetHabbo().Id, TicketId);
            Session.Shout("*Compra un boleto de lotería de la tienda*", 4);

            Session.GetHabbo().Credits -= Cost;
            Session.GetHabbo().UpdateCreditsBalance();

            using (var dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `rp_lottery` VALUES (@user, @ticket)");
                dbClient.AddParameter("user", Session.GetHabbo().Id);
                dbClient.AddParameter("ticket", TicketId);
                dbClient.RunQuery();
            }
            #endregion
        }
    }
}