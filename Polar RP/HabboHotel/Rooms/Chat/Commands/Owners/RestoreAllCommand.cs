using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class RestoreAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_restore_all"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Restaura todos los usuarios en línea que están muertos."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Execute
            int DeadUsers = 0;

            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null)
                    continue;

                if (client.GetRoleplay() == null)
                    continue;

                if (!client.GetRoleplay().IsDead)
                    continue;

                if (client.GetRoomUser() == null)
                    continue;

                DeadUsers++;

                client.GetRoleplay().IsDead = false;
                client.GetRoleplay().DeadTimeLeft = 0;
                client.GetRoleplay().ReplenishStats(true);
                client.SendWhisper("Un administrador le ha restaurado del hospital!");

            }

            Session.Shout("*Utiliza sus poderes divinos para restaurar a cualquier persona muerta en el hotel*", 23);
            Session.SendWhisper("Has restaurado correctamente a " + DeadUsers + " del hospital!", 1);

            #endregion

        }
    }
}
