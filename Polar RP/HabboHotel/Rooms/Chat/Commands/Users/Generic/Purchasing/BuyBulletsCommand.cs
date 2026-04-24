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
    class BuyBulletsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_purchasing_buy_bullets"; }
        }

        public string Parameters
        {
            get { return "%amount%"; }
        }

        public string Description
        {
            get { return "Compre balas para sus armas."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("¡Por favor ingrese la cantidad de puntos que desea comprar!", 1);
                return;
            }

            int Amount;
            if (!int.TryParse(Params[1], out Amount))
            {
                Session.SendWhisper("Ingrese una cantidad válida de viñetas que desea comprar!", 1);
                return;
            }

            if (Amount < 10)
            {
                Session.SendWhisper("¡Necesitas comprar al menos 10 balas a la vez!", 1);
                return;
            }
            if (Session.GetRoleplay().EquippedWeapon == null)
            {
                Session.SendWhisper("¡Debes tener el arma equipada para comprar balas!", 1);
                return;
            }

            Group Job = GroupManager.Jobs.Values.FirstOrDefault(x => x.Ranks.Count > 0 && x.Ranks.Values.FirstOrDefault().HasCommand("weapon"));

            if (Job == null || !Job.Ranks.Values.FirstOrDefault().CanWorkHere(Room.Id))
            {
                Session.SendWhisper("Usted debe estar dentro de la tienda de armas para comprar balas", 1);
                return;
            }

            int Cost = Convert.ToInt32(Math.Floor((double)Amount / 2));

            if (Session.GetHabbo().Credits < Cost)
            {
                Session.SendWhisper("No tienes suficiente dinero para comprar " + String.Format("{0:N0}", Amount) + " balas", 1);
                return;
            }
            #endregion

            #region Execute
            Session.GetHabbo().Credits -= Cost;
            Session.GetHabbo().UpdateCreditsBalance();
            Session.GetRoleplay().Bullets += Amount;
            Session.GetRoleplay().UpdateInteractingUserDialogues();
            Session.GetRoleplay().RefreshStatDialogue();
            Session.Shout("*Compra " + String.Format("{0:N0}", Amount) + " balas por $" + String.Format("{0:N0}", Cost) + "*", 4);
            return;
            #endregion
        }
    }
}