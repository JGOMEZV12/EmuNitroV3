using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.Database.Interfaces;
using Polar.Utilities;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators
{
    class UnmuteCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_mute_undo"; }
        }

        public string Parameters
        {
            get { return "%username%"; }
        }

        public string Description
        {
            get { return "Activar el sonido de un usuario actualmente silenciado."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduzca el nombre de usuario del usuario que desea activar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null || TargetClient.GetHabbo() == null)
            {
                Session.SendWhisper("Se ha producido un error al encontrar a ese usuario, tal vez no están en línea.", 1);
                return;
            }

            if (TargetClient.GetHabbo().TimeMuted <= 0)
            {
                Session.SendWhisper("¡Esta persona no está silenciada!", 1);
                return;
            }

            using (IQueryAdapter dbClient = PolarEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("UPDATE `users` SET `time_muted` = '0' WHERE `id` = '" + TargetClient.GetHabbo().Id + "' LIMIT 1");
            }

            TargetClient.GetHabbo().TimeMuted = 0;
            TargetClient.SendNotification("Usted ha sido un-silenciado por " + Session.GetHabbo().Username + "!");

            Session.Shout("*Utiliza sus dioses poderes y unmutes " + TargetClient.GetHabbo().Username + "*", 23);
            Session.SendWhisper("Ha desmutedo correctamente " + TargetClient.GetHabbo().Username + "!", 1);
        }
    }
}