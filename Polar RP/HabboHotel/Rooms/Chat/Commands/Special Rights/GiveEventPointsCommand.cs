using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class GiveEventPointsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_give_event_points"; }
        }

        public string Parameters
        {
            get { return "%username% %amount%"; }
        }

        public string Description
        {
            get { return "Proporciona al usuario la cantidad elegida de puntos de evento."; }
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
                TargetClient.GetHabbo().EventPoints += Amount;
                TargetClient.GetHabbo().UpdateEventPointsBalance();

                if (TargetClient != Session)
                {
                    TargetClient.SendWhisper("Acabas de ser galardonado " + String.Format("{0:N0}", Amount) + " Event Points from " + Session.GetHabbo().Username + ".", 1);
                    Session.SendWhisper("Acabas de dar " + TargetClient.GetHabbo().Username + " " + String.Format("{0:N0}", Amount) + " Puntos de evento que ponen su total a " + String.Format("{0:N0}", TargetClient.GetHabbo().EventPoints) + ".", 1);
                }
                else
                    Session.SendWhisper("Te acaba de dar " + String.Format("{0:N0}", Amount) + " Puntos de evento poniendo su total a " + String.Format("{0:N0}", TargetClient.GetHabbo().EventPoints) + ".", 1);
            }
            else if (Params[2] == "wipe" || Params[2] == "remove" || Params[2] == "take")
            {
                TargetClient.GetHabbo().EventPoints = 0;
                TargetClient.GetHabbo().UpdateEventPointsBalance();

                TargetClient.SendWhisper("Acabas de quitar tus Puntos de Evento " + Session.GetHabbo().Username + ".", 1);
                Session.SendWhisper("Acaba de eliminar todos los " + TargetClient.GetHabbo().Username + "'s puntos de eventos.", 1);
            }
            else
                Session.SendWhisper("por favor ingrese un número valido, o usar ':epoints (user) remove' Para quitar todos sus Puntos de Evento.", 1);
        }
    }
}
