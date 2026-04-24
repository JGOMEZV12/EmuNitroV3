using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class HtmlPageCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_open_div"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Abrir página para la comunidad"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, introduzca la página que desea abrir para todo el hotel!", 1);
                return;
            }

            PolarEnvironment.GetGame().GetWebEventManager().BroadCastWebEvent("event_htmlpage", "action:broadcast,page:" + Params[1].ToLower());
            Session.SendWhisper("Se ha enviado " + Params[1].ToLower() + " para toda la comunidad", 1);
            return;
        }
    }
}
