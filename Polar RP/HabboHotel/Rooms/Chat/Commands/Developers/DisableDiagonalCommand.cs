using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Polar.HabboRoleplay.Misc;
using Polar.HabboHotel.Rooms;
using Polar.HabboHotel.Items;
using Polar.HabboRoleplay.Bots;
using Polar.HabboRoleplay.Bots.Manager;
using Polar.Communication.Packets.Outgoing.Misc;
using Polar.Communication.Packets.Outgoing.Rooms.Notifications;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Developers
{
    class DisableDiagonalCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_disable_diagonal"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Disables diagonal walking in the current room."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Room.GetGameMap().DiagonalEnabled = !Room.GetGameMap().DiagonalEnabled;
            Session.SendWhisper("Se ha actualizado correctamente el valor booleano diagonal de esta sala.");
            Session.Shout("*Utiliza sus poderes divinos y actualiza la diagonal caminando por esta habitación*", 23);
        }
    }
}
