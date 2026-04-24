using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Users.Generic.Toggles
{
    class DisableWhispersCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_toggle_disable_whispers"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite habilitar o inhabilitar la capacidad de recibir susurros."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Session.GetHabbo().ReceiveWhispers = !Session.GetHabbo().ReceiveWhispers;
            Session.SendWhisper("Habilita/deshabilita " + (Session.GetHabbo().ReceiveWhispers ? "now" : "no longer") + " recibir susurros", 1);
        }
    }
}
