using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.HabboRoleplay.Misc;
using Polar.Communication.Packets.Outgoing.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Owners
{
    class ReleaseAllCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_release_all"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Libera a todos los usuarios en línea que están encarcelados."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Execute
            int JailedUsers = 0;

            foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null)
                    continue;

                if (client.GetRoleplay() == null)
                    continue;

                if (!client.GetRoleplay().IsJailed)
                    continue;

                if (client.GetRoomUser() == null)
                    continue;

                JailedUsers++;

                client.GetRoleplay().IsJailed = false;
                client.GetRoleplay().JailedTimeLeft = 0;
                client.SendWhisper("¡Un administrador te ha liberado de la cárcel!");
            }

            Session.Shout("*Utiliza sus poderes divinos para liberar a las personas encarceladas en la ciudad*", 23);
            Session.SendWhisper("Has liberado correctamente " + JailedUsers + " from jail!", 1);

            #endregion

        }
    }
}
