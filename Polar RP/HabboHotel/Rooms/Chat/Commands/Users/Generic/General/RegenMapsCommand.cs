using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.General
{
    class RegenMapsCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_general_regen_maps"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "¿Está roto el mapa del juego de tu habitación? ¡Fíjelo con este comando!"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Room.GetGameMap().GenerateMaps();
            Session.SendWhisper("Mapa de juego de esta sala con éxito re-generado.", 1);
        }
    }
}
