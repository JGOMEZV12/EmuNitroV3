using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;
using Polar.HabboHotel.GameClients;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class HtmlUIPageCommand : IChatCommand
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
            get { return "redirecciona a un usuario por su id"; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length < 3)
            {
                Session.SendWhisper("Introduzca el nombre de usuario seguido por una página para enviar.", 1);
                return;
            }

            GameClient TargetClient = PolarEnvironment.GetGame().GetClientManager().GetClientByUserID(Convert.ToInt32(Params[1]));

            string Data = "action:broadcast,page:" + Params[2].ToLower();
            PolarEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_htmlpage", Data);
            Session.SendWhisper("Abierto PÁGINA: '" + Params[2].ToLower() + "' para " + TargetClient.GetHabbo().Username, 1);
            return;
        }
    }
}
