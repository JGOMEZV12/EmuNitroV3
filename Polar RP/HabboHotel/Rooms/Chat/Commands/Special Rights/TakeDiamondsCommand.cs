using Polar.Communication.Packets.Outgoing.Rooms.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polar.HabboHotel.Rooms.Chat.Commands.SpecialRights
{
    class TakeDiamondsCommand : IChatCommand
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
                Session.SendWhisper("Debes ingresar el nombre de usuario y el monto que deseas quitarle..", 1);
                return;
            }

            var TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);

            if (TargetClient == null)
            {
                Session.SendWhisper("Este usuario no pudo ser encontrado! Quizás estén desconectados.", 1);
                return;
            }

            if (TargetClient.GetHabbo() == null || TargetClient.GetRoomUser() == null)
            {
                Session.SendWhisper("¡Este usuario no pudo ser encontrado! Quizás estén desconectados.", 1);
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
                TargetClient.GetHabbo().Diamonds -= Amount;
                TargetClient.GetHabbo().UpdateDiamondsBalance();

                Session.SendWhisper("Acabas de quitarle a " + TargetClient.GetHabbo().Username + " " + String.Format("{0:N0}", Amount) + " Rubies dejandolo en " + String.Format("{0:N0}", TargetClient.GetHabbo().Diamonds) + ".", 1);
            }
        }
    }
}
