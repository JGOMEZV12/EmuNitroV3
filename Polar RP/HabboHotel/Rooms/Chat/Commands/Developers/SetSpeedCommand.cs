using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class SetSpeedCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_set_speed"; }
        }

        public string Parameters
        {
            get { return "%value%"; }
        }

        public string Description
        {
            get { return "Establezca la velocidad de los rodillos en la habitación actual."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca un valor para la velocidad del roller.", 1);
                return;
            }

            int Speed;
            if (int.TryParse(Params[1], out Speed))
            {
                Session.GetHabbo().CurrentRoom.GetRoomItemHandler().SetSpeed(Speed);
                Session.Shout("*Utiliza sus poderes divinos y actualiza la velocidad de los rollers en la habitación*", 23);
            }
            else
                Session.SendWhisper("El importe no es válido. Ingrese un número válido..", 1);
        }
    }
}