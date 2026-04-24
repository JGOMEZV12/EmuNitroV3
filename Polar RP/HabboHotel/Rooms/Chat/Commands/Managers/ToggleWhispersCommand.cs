using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Managers
{
    class ToggleWhispersCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_toggle_whispers"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Le permite ignorar todos los susurros en la habitación, excepto por su cuenta."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            Session.GetHabbo().IgnorePublicWhispers = !Session.GetHabbo().IgnorePublicWhispers;
            Session.SendWhisper("Tu estas ignorando " + (Session.GetHabbo().IgnorePublicWhispers ? "now" : "no longer") + " los susurros del publico", 1);
        }
    }
}
