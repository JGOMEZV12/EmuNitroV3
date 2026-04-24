using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.GameClients;
using Polar.HabboRoleplay.Misc;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Offers
{
    class SellSkinsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_offers_sell_skins"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Vende todas tus pieles de caza obtenidas."; }
        }

        public async Task Execute(GameClient Session, Room Room, string[] Params)
        {
            if (string.IsNullOrEmpty(Session.GetRoleplay().HuntSkins))
            {
                Session.SendWhisper("No tienes ninguna piel de caza para vender.", 1);
                return;
            }

            string[] parts = Session.GetRoleplay().HuntSkins.Split('|');
            int totalSkins = 0;
            int totalPay = 0;
            int payPerSkin = 100;

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                string[] kv = part.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out int count))
                {
                    totalSkins += count;
                    totalPay += (count * payPerSkin);
                }
            }

            if (totalSkins <= 0)
            {
                Session.SendWhisper("No tienes ninguna piel de caza para vender.", 1);
                return;
            }

            Session.GetRoleplay().HuntSkins = "";
            Session.GetHabbo().Credits += totalPay;
            Session.GetHabbo().UpdateCreditsBalance();

            Session.Shout("*Vende " + totalSkins + " pieles de caza por $" + String.Format("{0:N0}", totalPay) + "*", 4);
            Session.SendWhisper("Has vendido " + totalSkins + " pieles y has recibido $" + String.Format("{0:N0}", totalPay) + ".", 1);
        }
    }
}