using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class GiveCoinsCommand : IChatCommand
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
                Session.SendWhisper("Debe ingresar el nombre de usuario y la cantidad que desea darles.", 1);
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
                TargetClient.GetHabbo().Credits += Amount;
                TargetClient.GetHabbo().UpdateCreditsBalance();

                if (TargetClient != Session)
                {
                    TargetClient.SendWhisper("Le has dado $" + String.Format("{0:N0}", Amount) + " para " + Session.GetHabbo().Username + ".", 1);
                    Session.SendWhisper("Acabas de dar " + TargetClient.GetHabbo().Username + " $" + String.Format("{0:N0}", Amount) + " putting their total to $" + String.Format("{0:N0}", TargetClient.GetHabbo().Credits) + ".", 1);
                }
                else
                    Session.SendWhisper("Te acaba de dar $" + String.Format("{0:N0}", Amount) + " Poniendo tu total a $" + String.Format("{0:N0}", TargetClient.GetHabbo().Credits) + ".", 1);
            }
            else if (Params[2] == "wipe" || Params[2] == "remove" || Params[2] == "take")
            {
                TargetClient.GetHabbo().Credits = 0;
                TargetClient.GetHabbo().UpdateCreditsBalance();

                TargetClient.SendWhisper("Acabas de quitar tu dinero actual " + Session.GetHabbo().Username + ".", 1);
                Session.SendWhisper("Acaba de eliminar todo el Dinero actual de " + TargetClient.GetHabbo().Username + "'s.", 1);
            }
            else
                Session.SendWhisper("Introduzca un número válido o utilice ' :coins (usuario) remove' para quitar todos sus créditos.", 1);
        }
    }
}
