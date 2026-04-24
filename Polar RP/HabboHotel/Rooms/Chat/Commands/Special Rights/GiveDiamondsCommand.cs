using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class GiveDiamondsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_diamonds"; }
        }

        public string Parameters
        {
            get { return "%username% %amount%"; }
        }

        public string Description
        {
            get { return "Le da al usuario la cantidad elegida de diamonds."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("You must enter the username and amount you wish to give them.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("This user could not be found! Maybe they are offline.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("This user could not be found! Maybe they are offline.", 1);
                return;
            }

            int Amount;
            if (int.TryParse(Params[2], out Amount))
            {
                if (Amount <= 0)
                {
                    Session.SendWhisper("El monto no puede contener -", 1);
                    return;
                }
                TargetClient.GetHabbo().Diamonds += Amount;
                TargetClient.GetHabbo().UpdateDiamondsBalance(Amount);

                if (TargetClient != Session)
                {
                    TargetClient.SendWhisper("You have just been awarded " + String.Format("{0:N0}", Amount) + " VIP TOkens from " + Session.GetHabbo().Username + ".", 1);
                    Session.SendWhisper("You have just given " + TargetClient.GetHabbo().Username + " " + String.Format("{0:N0}", Amount) + " VIP Tokens putting their total to " + String.Format("{0:N0}", TargetClient.GetHabbo().Diamonds) + ".", 1);
                }
                else
                    Session.SendWhisper("You have just given yourself " + String.Format("{0:N0}", Amount) + " VIP Tokens putting your total to " + String.Format("{0:N0}", TargetClient.GetHabbo().Diamonds) + ".", 1);
            }
            else if (Params[2] == "wipe" || Params[2] == "remove" || Params[2] == "take")
            {
                TargetClient.GetHabbo().Diamonds = 0;
                TargetClient.GetHabbo().UpdateDiamondsBalance(Amount);

                TargetClient.SendWhisper("You have just had your VIP Tokens taken away by " + Session.GetHabbo().Username + ".", 1);
                Session.SendWhisper("You have just removed all of " + TargetClient.GetHabbo().Username + "'s VIP Tokens.", 1);
            }
            else
                Session.SendWhisper("Please enter a valid number, or use ':diamonds (user) remove' to take away all their VIP Tokens.", 1);
        }
    }
}
