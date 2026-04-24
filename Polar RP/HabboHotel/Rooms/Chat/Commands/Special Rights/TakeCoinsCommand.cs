using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class TakeCoinsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_coins"; }
        }

        public string Parameters
        {
            get { return "%username% %amount%"; }
        }

        public string Description
        {
            get { return "Le da al usuario la cantidad de monedas elegida."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length != 3)
            {
                Session.SendWhisper("Debe ingresar el nombre de usuario y la cantidad que desea quitarles.", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Quizás están fuera de línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("Este usuario no se pudo encontrar! Quizás están fuera de línea.", 1);
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
                TargetClient.GetHabbo().Credits -= Amount;
                TargetClient.GetHabbo().UpdateCreditsBalance();

                //TargetClient.SendWhisper("Le has quitado $" + String.Format("{0:N0}", Amount) + " para " + Session.GetHabbo().Username + ".", 1);
                Session.SendWhisper("Le has quitado a " + TargetClient.GetHabbo().Username + " $" + String.Format("{0:N0}", Amount) + " para un total de $" + String.Format("{0:N0}", TargetClient.GetHabbo().Credits) + ".", 1);
                
            }
        }
    }
}
