using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Ambassadors
{
    class AmbassadorAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_alert_ambassador"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Sends a message typed by you to the current online ambassadors."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Please enter a message to send.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            PolarEnvironment.GetGame().GetClientManager().AmbassadorWhisperAlert(Message, Session);
        }
    }
}
