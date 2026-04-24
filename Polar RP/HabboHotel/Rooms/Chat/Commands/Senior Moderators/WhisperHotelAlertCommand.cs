using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Polar.Communication.Packets.Outgoing.Moderation;

namespace Polar.HabboHotel.Rooms.Chat.Commands.Moderators.Seniors
{
    class WhisperHotelAlertCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_hotel_alert_whisper"; }
        }

        public string Parameters
        {
            get { return "%message%"; }
        }

        public string Description
        {
            get { return "Enviar alerta a toda la ciudad."; }
        }

        public async Task Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Params.Length == 1)
            {
                Session.SendWhisper("Introduce un mensaje.", 1);
                return;
            }

            string Message = CommandManager.MergeParams(Params, 1);

            lock (PolarEnvironment.GetGame().GetClientManager().GetClients)
            {
                foreach (var client in PolarEnvironment.GetGame().GetClientManager().GetClients.ToList())
                {
                    if (client == null || client.GetHabbo() == null)
                        continue;

                    client.SendWhisper("[NOTIFICACIÓN IMPORTANTE] [" + Session.GetHabbo().Username + "] " + Message, 33);
                }
            }
        }
    }
}
