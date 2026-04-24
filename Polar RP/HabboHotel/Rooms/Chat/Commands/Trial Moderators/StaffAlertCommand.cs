using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Trials
{
    class StaffAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_alert_staff"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "envia alerta a todo el staff."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("introduce un mensaje.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            PolarEnvironment.GetGame().GetClientManager().StaffWhisperAlert(Message, Session);
        }
    }
}
