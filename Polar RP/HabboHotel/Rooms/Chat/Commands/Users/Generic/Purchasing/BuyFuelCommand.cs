using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Notifications;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Rooms.Chat.Styles;
using Polar.HabboRoleplay.RoleplayUsers;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Groups;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Purchasing
{
    class BuyFuelCommandx : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_purchasing_buy_fuel"; }
        }

        public string Parameters
        {
            get { return "%amount%"; }
        }

        public string Description
        {
            get { return "Compra gasolina en la 12."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Por favor ingrese la cantidad de combustible que le gustaría comprar!", 1);
                return;
            }

            int Amount;
            if (!int.TryParse(Params[1], out Amount))
            {
                Session.SendWhisper("¡Por favor ingrese una cantidad válida de combustible que le gustaría comprar!", 1);
                return;
            }

            if (Amount < 10)
            {
                Session.SendWhisper("¡Usted necesita comprar al menos 10 galones de combustible a la vez!", 1);
                return;
            }

            Group Job = GroupManager.Jobs.Values.FirstOrDefault(x => x.Ranks.Count > 0 && x.Ranks.Values.FirstOrDefault().HasCommand("car"));

            if (Job == null || !Job.Ranks.Values.FirstOrDefault().CanWorkHere(Room.Id))
            {
                Session.SendWhisper("Usted debe estar dentro de la tienda de coches para comprar combustible", 1);
                return;
            }

            int Cost = Convert.ToInt32(Math.Floor((double)(Amount * 2) / 3));

            if (Session.GetHabbo().Credits < Cost)
            {
                Session.SendWhisper("No tienes suficiente dinero para comprar " + String.Format("{0:N0}", Amount) + " Galones de combustible", 1);
                return;
            }
            #endregion

            #region Execute
            Session.GetHabbo().Credits -= Cost;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().CarFuel += Amount;

            Session.Shout("*Compra " + String.Format("{0:N0}", Amount) + " galones de gasolina`por $" + String.Format("{0:N0}", Cost) + "*", 4);
            return;
            #endregion
        }
    }
}